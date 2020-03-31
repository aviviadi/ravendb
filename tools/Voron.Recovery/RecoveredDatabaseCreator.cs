using System;
using System.IO;
using Raven.Client.Documents.Operations.Attachments;
using Raven.Client.Documents.Operations.Counters;
using Raven.Client.Documents.Session;
using Raven.Client.Extensions;
using Raven.Client.Json;
using Raven.Server.Documents;
using Raven.Server.ServerWide.Context;
using Sparrow.Json;
using Sparrow.Json.Parsing;
using Sparrow.Logging;
using Sparrow.Server;
using Sparrow.Threading;
using Voron.Data;
using static Raven.Client.Constants.Documents;

namespace Voron.Recovery
{
    public class RecoveredDatabaseCreator : IDisposable
    {
        public static RecoveredDatabaseCreator RecoveredDbTools(DocumentDatabase documentDatabase, Logger logger) =>
            new RecoveredDatabaseCreator(documentDatabase, logger);

        private readonly Logger _logger;
        private readonly DocumentsOperationContext _context;
        private readonly IDisposable _contextDisposal;
        private readonly ByteStringContext _byteStringContext;
        private readonly Slice _attachmentsSlice;
        private readonly RecoveryDocumentStorageBatch _recoveryDocumentsStorage;

        private static string GetOrphanAttachmentDocId(string hash) => $"OrphanAttachment/{hash}";
        private static string GetOrphanRevisionDocId(string revisionId) => $"OrphanRevision/{revisionId}/{Guid.NewGuid()}";
        private static string GetOrphanCounterDocId(string documentId) => $"OrphanCounter/{documentId}";

        private const string RecoveryOrphanAttachmentsCollection = "RecoveryOrphanAttachments";
        private const string RecoveryOrphanCountersCollection = "RecoveryOrphanCounters";

        private static class Constants
        {
#pragma warning disable 649
            public static string Name;
            public static string ContentType;
            public static string OriginalDocId;
#pragma warning restore 649
        }

        private RecoveredDatabaseCreator(DocumentDatabase documentDatabase, Logger logger)
        {
            _logger = logger;
            _contextDisposal = documentDatabase.DocumentsStorage.ContextPool.AllocateOperationContext(out _context);
            _byteStringContext = new ByteStringContext(SharedMultipleUseFlag.None);
            Slice.From(_byteStringContext, "Attachments", ByteStringType.Immutable, out _attachmentsSlice);
            _recoveryDocumentsStorage = new RecoveryDocumentStorageBatch(documentDatabase, _context);
        }

        public void Dispose()
        {
            _contextDisposal.Dispose();
            _byteStringContext.Dispose();
            _recoveryDocumentsStorage.Dispose();
        }

        public void WriteDocument(Document document)
        {
            // store document with only already exist attachments, and save the reset to be merged in the end
            StripAndStoreDocument(document, out var hasRevisions, out var counters, out var attachments);
            StoreForLaterDocumentItems(document.Id, hasRevisions, counters, attachments);
        }

        public void WriteRevision(Document revision)
        {
            // revisions cannot be updated later with attachments, so store them for later unless they do not have unseen attachments and their parent doc already exists
            if (TryStoreRevision(revision) == false) // counters are only a snapshot in revisions so nothing to do with it
                StoreForLaterRevision(revision);

        }

        public void WriteConflict(DocumentConflict conflict)
        {
            // StripConflict(conflict, out var hasRevisions, out var counters, out var attachments); // TODO: needed?
            StoreConflict(conflict);

            // StoreForLaterConflictItems(conflict.Id, hasRevisions, counters, attachments);
        }

        public void WriteCounterItem(CounterGroupDetail counterGroup)
        {
            if (TryStoreCounterGroup(counterGroup) == false)
                StoreForLaterCounter(counterGroup);
        }

        public void WriteAttachment(string hash, string name, string contentType, Stream attachmentStream, long totalSize)
        {
            if (TryWriteAttachment(hash, name, contentType, attachmentStream, totalSize) == false)
                StoreForLaterAttachment(hash, name, contentType, attachmentStream, totalSize);
        }

        ///////////////////////////

        private bool TryStoreCounterGroup(CounterGroupDetail counterGroup)
        {
            var doc = _recoveryDocumentsStorage.Get(counterGroup.DocumentId);
            if (doc == null)
                return false;

            _recoveryDocumentsStorage.PutCounters(doc.Id.ToString(), CollectionName.GetCollectionName(doc.Data), counterGroup.ChangeVector, counterGroup.Values);

            return true;
        }

        private bool TryWriteAttachment(string hash, string name, string contentType, Stream attachmentStream, in long totalSize)
        {
            var orphanAttachmentDocId = GetOrphanAttachmentDocId(hash);
            var orphanAttachmentDocument = _recoveryDocumentsStorage.Get(orphanAttachmentDocId);
            if (orphanAttachmentDocument == null)
                return false;

            // check which documents already seen for this attachment and attach it to them
            orphanAttachmentDocument.Data.Modifications = new DynamicJsonValue(orphanAttachmentDocument.Data);
            foreach (var docId in orphanAttachmentDocument.Data.GetPropertyNames())
            {
                if (docId.Equals(Metadata.Key) || docId.Equals(Metadata.Collection))
                    continue;
                if (orphanAttachmentDocument.Data.TryGetMember(docId, out var attachmentDataObj) == false)
                    continue;
                if (!(attachmentDataObj is BlittableJsonReaderObject attachmentData))
                    continue;

                var seenDoc = _recoveryDocumentsStorage.Get(docId);
                if (seenDoc == null)
                    continue;

                if (attachmentData.TryGet(nameof(Constants.Name), out string originalName) == false)
                    originalName = name;
                if (attachmentData.TryGet(nameof(Constants.ContentType), out string originalContentType) == false)
                    originalContentType = contentType;

                orphanAttachmentDocument.Data.Modifications.Remove(docId);
                var attachmentDetails = _recoveryDocumentsStorage.PutAttachment(docId, originalName, originalContentType, hash, attachmentStream);

                if (attachmentDetails.Size != totalSize)
                {
                    // TODO : Log("Attachment " + originalName + " of doc " + docId + " stream size is " + attachmentDetails.Size + " which is not as reported in datafile: " + totalSize);
                }
            }

            using (var newDocument = _context.ReadObject(orphanAttachmentDocument.Data, orphanAttachmentDocument.Id,
                BlittableJsonDocumentBuilder.UsageMode.ToDisk))
            {
                if (newDocument.GetPropertyNames().Length == 1) // 1 for @metadata
                {
                    _recoveryDocumentsStorage.Delete(orphanAttachmentDocument.Id);
                }
                else
                {
                    var metadata = orphanAttachmentDocument.Data.GetMetadata();
                    if (metadata != null)
                        metadata.Modifications = new DynamicJsonValue(metadata) {[Metadata.Collection] = RecoveryOrphanAttachmentsCollection};
                    _recoveryDocumentsStorage.Put(orphanAttachmentDocument.Id, newDocument);
                }
            }

            return true;
        }

        private void StoreForLaterAttachment(string hash, string name, string contentType, Stream attachmentStream, in long totalSize)
        {
            var orphanAttachmentDocId = GetOrphanAttachmentDocId(hash);
            VoronStream existingStream = null;

            var tree = _recoveryDocumentsStorage.CreateTree(_attachmentsSlice);
            existingStream = tree.ReadStream(hash);

            if (existingStream != null)
                return;

            using (var newDocument = _context.ReadObject(
                new DynamicJsonValue
                {
                    [Metadata.Key] = new DynamicJsonValue {[Metadata.Collection] = RecoveryOrphanAttachmentsCollection}
                },
                orphanAttachmentDocId, BlittableJsonDocumentBuilder.UsageMode.ToDisk))
            {
                _recoveryDocumentsStorage.Put(orphanAttachmentDocId, newDocument, null);
                _recoveryDocumentsStorage.PutAttachment(orphanAttachmentDocId, name, contentType, hash, attachmentStream);
            }
        }

        private void StoreForLaterCounter(CounterGroupDetail counterGroupDetail)
        {
            var orphanCounterDocId = GetOrphanCounterDocId(counterGroupDetail.DocumentId);
            var orphanCounterDocument = _recoveryDocumentsStorage.Get(orphanCounterDocId);
            if (orphanCounterDocument == null)
            {
                using (var doc = _context.ReadObject(
                    new DynamicJsonValue
                    {
                        [Metadata.Key] =
                            new DynamicJsonValue {[Metadata.Collection] = RecoveryOrphanCountersCollection}
                    }, orphanCounterDocId, BlittableJsonDocumentBuilder.UsageMode.ToDisk)
                )
                {
                    _recoveryDocumentsStorage.Put(orphanCounterDocId, doc, null);
                }
            }
            _recoveryDocumentsStorage.PutCounters(
                orphanCounterDocId, RecoveryOrphanCountersCollection, counterGroupDetail.ChangeVector, counterGroupDetail.Values);
        }

        private void StripAndStoreDocument(Document document, out bool hasRevisions, out bool hasCounters, out IMetadataDictionary[] attachments)
        {
            var metadata = document.Data.GetMetadata();
            if (metadata == null)
                throw new Exception($"No metadata for {document.Id}, cannot recover this document");
            var metadataDictionary = new MetadataAsDictionary(metadata);

            hasRevisions = document.Flags.HasFlag(DocumentFlags.HasRevisions);
            hasCounters = document.Flags.HasFlag(DocumentFlags.HasCounters);
            attachments = document.Flags.HasFlag(DocumentFlags.HasAttachments) ? metadataDictionary.GetObjects(Metadata.Attachments) : null;

            document.Flags &= DocumentFlags.HasRevisions;
            document.Flags &= DocumentFlags.HasCounters;
            document.Flags &= DocumentFlags.HasAttachments;

            metadata.Modifications = new DynamicJsonValue(metadata);
            metadata.Modifications.Remove(Metadata.Attachments);

            using (document.Data)
                document.Data = _context.ReadObject(document.Data, document.Id, BlittableJsonDocumentBuilder.UsageMode.ToDisk);

            _recoveryDocumentsStorage.Put(document.Id, document.Data, document.LastModified.Ticks, document.Flags);
        }

        private void StoreForLaterDocumentItems(LazyStringValue documentId, in bool hasRevisions, in bool counters, IMetadataDictionary[] attachments)
        {
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    var hash = attachment.GetString(nameof(AttachmentName.Hash));
                    var name = attachment.GetString(nameof(AttachmentName.Name));
                    var contentType = attachment.GetString(nameof(AttachmentName.ContentType));

                    if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(name))
                    {
                        // TODO: Log($"Document {document.Id} has attachment flag set with empty hash / name");
                        continue;
                    }

                    var orphanAttachmentDocId = GetOrphanAttachmentDocId(hash);
                    using (var doc = _context.ReadObject(
                        new DynamicJsonValue
                        {
                            [documentId] = new DynamicJsonValue {[nameof(Constants.Name)] = name, [nameof(Constants.ContentType)] = contentType},
                            [Metadata.Key] =
                                new DynamicJsonValue {[Metadata.Collection] = RecoveryOrphanAttachmentsCollection}
                        }, orphanAttachmentDocId, BlittableJsonDocumentBuilder.UsageMode.ToDisk)
                    )
                    {
                        _recoveryDocumentsStorage.Put(orphanAttachmentDocId, doc, null);
                    }
                }
            }
        }

        private bool TryStoreRevision(Document revision)
        {
            if (_recoveryDocumentsStorage.Get(revision.Id) == null)
                return false;

            if (revision.Flags.Contain(DocumentFlags.HasAttachments))
            {
                var metadata = revision.Data.GetMetadata();
                var metadataDictionary = new MetadataAsDictionary(metadata);

                var attachments = metadataDictionary.GetObjects(Metadata.Attachments);
                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        var tree = _recoveryDocumentsStorage.CreateTree(_attachmentsSlice);
                        var hash = attachment.GetString(nameof(AttachmentName.Hash));
                        var existingStream = tree.ReadStream(hash);
                        if (existingStream == null)
                            return false; // revisions cannot be updated later so we need everything in place
                    }
                }
            }

            _recoveryDocumentsStorage.PutRevision(revision.Id, revision.Flags, revision.NonPersistentFlags, revision.LastModified.Ticks);
            return true;
        }

        private void StoreForLaterRevision(Document revision)
        {
            var orphanRevisionDocId = GetOrphanRevisionDocId(revision.Id);
            using (var doc = revision.Data.Clone(_context))
            {
                _recoveryDocumentsStorage.Put(orphanRevisionDocId, doc, null);
            }
        }

        private void StoreConflict(DocumentConflict conflict)
        {
            _recoveryDocumentsStorage.AddConflict(
                conflict.Id, conflict.LastModified.Ticks, conflict.Doc, conflict.ChangeVector, conflict.Collection, conflict.Flags);
        }
    }
}
