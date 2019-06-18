using System;
using System.Collections.Generic;
using System.Threading;
using LoadCompress.Core.GZipFast.Data;

namespace LoadCompress.Core.Notification
{
    internal class ProgressNotifier: IProgressNotificationService, IProgressNotificationClient
    {
        private readonly Action<CompressionStatus> _eventHandler;
        private readonly Func<GZipBlock, long> _sizeRetrievingFunc;
        private readonly object _progressLocker;
        private bool _isRunning;
        private readonly Queue<QueuedNotification> _pendingProgressEvents;
        private Thread _notificationThread;

        internal ProgressNotifier(Action<CompressionStatus> eventHandler, Func<GZipBlock, long> sizeRetrievingFunc,
            bool startImmediately)
        {
            _eventHandler = eventHandler;
            _sizeRetrievingFunc = sizeRetrievingFunc;
            _progressLocker = new object();
            _isRunning = false;
            _pendingProgressEvents = new Queue<QueuedNotification>();

            if (startImmediately)
            {
                Start(false);
            }
        }

        public bool TryNotify(GZipBlock block, int totalBlocks)
        {
            if (!_isRunning)
                return false;

            lock (_progressLocker)
            {
                _pendingProgressEvents.Enqueue(new QueuedNotification(block, totalBlocks));
                Monitor.Pulse(_progressLocker);
                return true;
            }
        }

        public void Start(bool failIfAlreadyStarted)
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
                NotificationEventLoop();
            }
        }

        public void Stop(bool failIfAlreadyStopped)
        {
            if (!_isRunning && failIfAlreadyStopped)
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

        internal void NotificationEventLoop()
        {
            long bytesProceeded = 0;
            var blocksProceeded = 0;

            lock (_progressLocker)
            {
                while (true)
                {
                    while (_isRunning && _pendingProgressEvents.TryDequeue(out var notification))
                    {
                        bytesProceeded += _sizeRetrievingFunc(notification.Block);
                        blocksProceeded++;

                        // Callback may block execution, but all events will be actual.
                        _eventHandler(new CompressionStatus(bytesProceeded, blocksProceeded, notification.TotalBlocks));
                    }

                    if(!_isRunning)
                        break;

                    Monitor.Wait(_progressLocker);
                }
            }
        }

        public void Dispose()
        {
            Stop(false);
        }
    }
}