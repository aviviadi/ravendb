using System;
using System.Collections.Generic;
using System.IO;
using Raven.Client.Documents.Operations.Attachments;
using Raven.Server.Documents;
using Raven.Server.ServerWide.Context;
using Sparrow.Json;
using Voron.Data.BTrees;

namespace Voron.Recovery
{
    public class RecoveryDocumentStorageBatch : IDisposable
    {
        private class BatchTransaction : IDisposable
        {
            private readonly RecoveryDocumentStorageBatch _parent;
            private readonly bool _doNotCloseTx;
            private Queue<Tree> Trees => _parent._trees;
            private long OperationsCounter => _parent._operationsCounter;
            private DocumentsTransaction Tx
            {
                get => _parent._tx;
                set => _parent._tx = value;
            }

            public BatchTransaction(RecoveryDocumentStorageBatch recoveryDocumentStorageBatch, DocumentsOperationContext context, bool doNotCloseTx = false)
            {
                _doNotCloseTx = doNotCloseTx;
                _parent = recoveryDocumentStorageBatch;
                recoveryDocumentStorageBatch._operationsCounter++;
                recoveryDocumentStorageBatch._tx ??= context.OpenWriteTransaction();
            }

            public void Dispose()
            {
                if (_doNotCloseTx)
                    return;

                if (Trees.Count < 32 && OperationsCounter % 10 != 1)
                    return;

                Tx.Commit();

                foreach (var tree in Trees)
                    tree.Dispose();
                Trees.Clear();

                Tx.Dispose();
                Tx = null;
            }
        }

        public RecoveryDocumentStorageBatch(DocumentDatabase documentDatabase, DocumentsOperationContext context)
        {
            _documentDatabase = documentDatabase;
            _context = context;
        }

        private readonly DocumentDatabase _documentDatabase;
        private readonly DocumentsOperationContext _context;
        private int _operationsCounter;
        private DocumentsTransaction _tx;
        private readonly Queue<Tree> _trees = new Queue<Tree>();

        public Document Get(string id)
        {
            using (new BatchTransaction(this, _context, true))
            {
                return _documentDatabase.DocumentsStorage.Get(
                    _context, id);
            }
        }

        public void PutCounters( string documentId, string collection, string changeVector, BlittableJsonReaderObject sourceData)
        {
            using (new BatchTransaction(this, _context))
            {
                _documentDatabase.DocumentsStorage.CountersStorage.PutCounters(
                    _context, documentId, collection, changeVector, sourceData);
            }
        }


        public void Dispose()
        {
            foreach (var tree in _trees)
                tree.Dispose();
            _trees.Clear();
            _tx?.Commit();
            _tx?.Dispose();
            _tx = null;
        }

        public AttachmentDetails PutAttachment(string documentId, string name, string contentType, string hash, Stream stream)
        {
            using (new BatchTransaction(this, _context))
            {
                return _documentDatabase.DocumentsStorage.AttachmentsStorage.PutAttachment(
                    _context, documentId, name, contentType, hash, null, stream);
            }
        }

        public void Delete(LazyStringValue id)
        {
            using (new BatchTransaction(this, _context))
            {
                using (DocumentIdWorker.GetSliceFromId(_context, id, out Slice lowerId))
                {
                    _documentDatabase.DocumentsStorage.Delete(
                        _context, lowerId, id, null, null, null, null, NonPersistentDocumentFlags.FromSmuggler);
                }
            }
        }

        public DocumentsStorage.PutOperationResults Put(LazyStringValue id, BlittableJsonReaderObject document)
        {
            using (new BatchTransaction(this, _context))
            {
                return _documentDatabase.DocumentsStorage.Put(
                    _context, id, null, document, nonPersistentFlags: NonPersistentDocumentFlags.FromSmuggler);
            }
        }

        public DocumentsStorage.PutOperationResults Put(string id, BlittableJsonReaderObject document, long? lastModifiedTicks, DocumentFlags flags = DocumentFlags.None)
        {
            using (new BatchTransaction(this, _context))
            {
                return _documentDatabase.DocumentsStorage.Put(
                    _context, id, null, document, lastModifiedTicks, null, flags, NonPersistentDocumentFlags.FromSmuggler);
            }
        }

        // _documentsStorage.RevisionsStorage.Put(_context, revision.Id, null, revision.Flags, revision.NonPersistentFlags, null, revision.LastModified.Ticks);
        public bool PutRevision(LazyStringValue revisionId, DocumentFlags revisionFlags, NonPersistentDocumentFlags revisionNonPersistentFlags, long lastModifiedTicks)
        {
            using (new BatchTransaction(this, _context))
            {
                return _documentDatabase.DocumentsStorage.RevisionsStorage.Put(
                    _context, revisionId, null, revisionFlags, revisionNonPersistentFlags | NonPersistentDocumentFlags.FromSmuggler, null, lastModifiedTicks);
            }
        }

        public void AddConflict(LazyStringValue conflictId, long lastModifiedTicks, BlittableJsonReaderObject conflictDoc, string conflictChangeVector, LazyStringValue conflictCollection, DocumentFlags conflictFlags)
        {
            using (new BatchTransaction(this, _context))
            {
                _documentDatabase.DocumentsStorage.ConflictsStorage.AddConflict(
                    _context, conflictId, lastModifiedTicks, conflictDoc, conflictChangeVector, conflictCollection, conflictFlags, NonPersistentDocumentFlags.FromSmuggler);
            }
        }

        public Tree CreateTree(Slice slice)
        {
            _tx ??= _context.OpenWriteTransaction();
            var tree = _context.Transaction.InnerTransaction.CreateTree(slice);
            _trees.Enqueue(tree);
            return tree;
        }
    }
}
