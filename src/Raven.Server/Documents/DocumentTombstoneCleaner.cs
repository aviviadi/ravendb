﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Server.Background;
using Raven.Server.ServerWide.Context;
using Sparrow.Logging;

namespace Raven.Server.Documents
{
    public class DocumentTombstoneCleaner : BackgroundWorkBase
    {
        private static Logger _logger;

        private readonly SemaphoreSlim _subscriptionsLocker = new SemaphoreSlim(1, 1);

        private readonly DocumentDatabase _documentDatabase;

        private readonly HashSet<IDocumentTombstoneAware> _subscriptions = new HashSet<IDocumentTombstoneAware>();

        public DocumentTombstoneCleaner(DocumentDatabase documentDatabase) : base(documentDatabase.Name, documentDatabase.DatabaseShutdown)
        {
            _documentDatabase = documentDatabase;
            _logger = LoggingSource.Instance.GetLogger<DocumentTombstoneCleaner>(_documentDatabase.Name);
        }

        public void Subscribe(IDocumentTombstoneAware subscription)
        {
            _subscriptionsLocker.Wait();

            try
            {
                _subscriptions.Add(subscription);
            }
            finally
            {
                _subscriptionsLocker.Release();
            }
        }

        public void Unsubscribe(IDocumentTombstoneAware subscription)
        {
            _subscriptionsLocker.Wait();

            try
            {
                _subscriptions.Remove(subscription);
            }
            finally
            {
                _subscriptionsLocker.Release();
            }
        }

        protected override async Task<bool> DoWork()
        {
            if (await WaitAsync(_documentDatabase.Configuration.Tombstones.Interval.AsTimeSpan) == false)
                return false;

            await ExecuteCleanup();

            return true;
        }

        internal async Task ExecuteCleanup()
        {
            try
            {
                if (CancellationToken.IsCancellationRequested)
                    return;

                var tombstones = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                var storageEnvironment = _documentDatabase.DocumentsStorage.Environment;
                if (storageEnvironment == null) // doc storage was disposed before us?
                    return;

                using (var tx = storageEnvironment.ReadTransaction())
                {
                    foreach (var tombstoneCollection in _documentDatabase.DocumentsStorage.GetTombstoneCollections(tx))
                    {
                        tombstones[tombstoneCollection] = long.MaxValue;
                    }
                }

                if (tombstones.Count == 0)
                    return;

                long minAllDocsEtag = long.MaxValue;

                await _subscriptionsLocker.WaitAsync();

                try
                {
                    foreach (var subscription in _subscriptions)
                    {
                        foreach (var tombstone in subscription.GetLastProcessedDocumentTombstonesPerCollection())
                        {
                            if (tombstone.Key == Constants.Documents.Replication.AllDocumentsCollection)
                            {
                                minAllDocsEtag = Math.Min(tombstone.Value, minAllDocsEtag);
                                break;
                            }

                            long v;
                            if (tombstones.TryGetValue(tombstone.Key, out v) == false)
                                tombstones[tombstone.Key] = tombstone.Value;
                            else
                                tombstones[tombstone.Key] = Math.Min(tombstone.Value, v);
                        }
                    }
                }
                finally
                {
                    _subscriptionsLocker.Release();
                }

                await _documentDatabase.TxMerger.Enqueue(new DeleteTombstonesCommand(tombstones, minAllDocsEtag, _documentDatabase));
            }
            catch (Exception e)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info($"Failed to execute tombstone cleanup on {_documentDatabase.Name}", e);
            }
        }

        private class DeleteTombstonesCommand : TransactionOperationsMerger.MergedTransactionCommand
        {
            private readonly Dictionary<string, long> _tombstones;
            private readonly long _minAllDocsEtag;
            private readonly DocumentDatabase _database;

            public DeleteTombstonesCommand(Dictionary<string, long> tombstones, long minAllDocsEtag, DocumentDatabase database)
            {
                _tombstones = tombstones;
                _minAllDocsEtag = minAllDocsEtag;
                _database = database;
            }

            public override int Execute(DocumentsOperationContext context)
            {
                var deletionCount = 0;

                foreach (var tombstone in _tombstones)
                {
                    var minTombstoneValue = Math.Min(tombstone.Value, _minAllDocsEtag);
                    if (minTombstoneValue <= 0)
                        continue;

                    if (_database.DatabaseShutdown.IsCancellationRequested)
                        break;

                    deletionCount++;

                    try
                    {
                        _database.DocumentsStorage.DeleteTombstonesBefore(tombstone.Key, minTombstoneValue, context);
                    }
                    catch (Exception e)
                    {
                        if (_logger.IsInfoEnabled)
                            _logger.Info( $"Could not delete tombstones for '{tombstone.Key}' collection before '{minTombstoneValue}' etag.", e);

                        throw;
                    }
                }

                return deletionCount;
            }
        }
    }

    public interface IDocumentTombstoneAware
    {
        Dictionary<string, long> GetLastProcessedDocumentTombstonesPerCollection();
    }
}