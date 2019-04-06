using System;
using System.Collections.Generic;
using System.Threading;
using LoadCompress.Core.GZipFast.Data;

namespace LoadCompress.Core
{
    internal class ProgressNotifier
    {
        private readonly Action<CompressionStatus> _eventHandler;
        private readonly Func<GZipBlock, long> _sizeRetrievingPredicate;
        private readonly object _progressLocker;
        private bool _isRunning;
        private readonly Queue<GZipBlock> _pendingProgressEvents;
        private Thread _notificationThread;

        internal ProgressNotifier(Action<CompressionStatus> eventHandler, Func<GZipBlock, long> sizeRetrievingPredicate)
        {
            _eventHandler = eventHandler;
            _sizeRetrievingPredicate = sizeRetrievingPredicate;
            _progressLocker = new object();
            _isRunning = false;
            _pendingProgressEvents = new Queue<GZipBlock>();
        }

        internal bool TryNotify(GZipBlock block)
        {
            if (!_isRunning)
                return false;

            lock (_progressLocker)
            {
                _pendingProgressEvents.Enqueue(block);
                Monitor.Pulse(_progressLocker);
                return true;
            }
        }

        internal void Start(int totalBlocks)
        {
            if (_isRunning)
                throw new InvalidOperationException("Notification process is already running");
            _isRunning = true;

            _notificationThread = new Thread(StartLoop)
            {
                Name = "Notifier"
            };
            _notificationThread.Start();

            void StartLoop()
            {
                NotificationEventLoop(totalBlocks);
            }
        }

        internal void Stop()
        {
            if (!_isRunning)
                throw new InvalidOperationException("Notification process is already stopped");

            lock (_progressLocker)
            {
                _pendingProgressEvents.Clear();
                Monitor.Pulse(_progressLocker);
                _isRunning = false;
            }

            _notificationThread.Join();
            _notificationThread = null;
        }

        internal void NotificationEventLoop(int totalBlocks)
        {
            long bytesProceeded = 0;
            var blocksProceeded = 0;

            lock (_progressLocker)
            {
                while (true)
                {
                    while (_isRunning && _pendingProgressEvents.TryDequeue(out var block))
                    {
                        bytesProceeded += _sizeRetrievingPredicate(block);
                        blocksProceeded++;

                        // Callback may block execution, but all events will be actual.
                        _eventHandler(new CompressionStatus(bytesProceeded, blocksProceeded, totalBlocks));
                    }

                    if(!_isRunning)
                        break;

                    Monitor.Wait(_progressLocker);
                }
            }
        }
    }
}