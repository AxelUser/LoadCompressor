using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Handlers;
using LoadCompress.Core.GZipFast.Interfaces;
using LoadCompress.Core.GZipFast.Queues;
using LoadCompress.Core.Notification;

namespace LoadCompress.Core.GZipFast.Runners
{
    internal class GZipOperationRunner: IGZipOperationRunner, IDisposable
    {
        private readonly IHandlerFactory _handlerFactory;
        private readonly IProgressNotificationClient _notificationClient;
        private readonly GZipTransformationQueue _transformationQueue;
        private readonly List<Exception> _exceptions;
        private readonly CountdownEvent _onFinishedEvent;
        private readonly WorkersPool _pool;

        private readonly Dictionary<int, byte[]> _rentedBuffers;

        private volatile bool _isFailed;
        private readonly object _savingLocker = new object();

        internal GZipOperationRunner(IHandlerFactory handlerFactory, IProgressNotificationClient notificationClient, WorkersPool pool)
        {
            _rentedBuffers = new Dictionary<int, byte[]>();
            _handlerFactory = handlerFactory;
            _notificationClient = notificationClient;
            _pool = pool;
            _transformationQueue = new GZipTransformationQueue(_pool, HandleError);
            _exceptions = new List<Exception>();
            _onFinishedEvent = new CountdownEvent(0);
            _isFailed = false;
        }

        /// <inheritdoc />
        public void RunForCompletion(GZipHeader header, Stream source, Stream destination)
        {
            var handler = _handlerFactory.Create();
            var innerResultHandler = handler.CreateResultHandler(destination, header);
            _isFailed = false;
            var totalBlocks = header.BlocksCount;
            var queue = handler.CreateQueue(_transformationQueue);

            SetupCompletionEvent(header.BlocksCount);

            for (var i = 0; i < header.BlocksCount; i++)
            {
                if (_isFailed)
                    break;
                var sourceBuffer = ArrayPool<byte>.Shared.Rent(handler.GetBlockSize(header, i));

                var bytesRead = source.Read(sourceBuffer.AsSpan(0, handler.GetBlockSize(header, i)));

                var block = handler.GetBlock(header, i, bytesRead);
                lock (_rentedBuffers)
                {
                    _rentedBuffers[block.Id] = sourceBuffer;
                }
                queue.Enqueue(block, sourceBuffer.AsMemory(0, bytesRead), HandleResult);
            }

            WaitForCompletion();

            if (_exceptions.Count > 0)
                throw new AggregateException("Failed to compress", _exceptions);

            handler.OnCompletedSuccessfully(destination, header);

            void HandleResult(GZipBlock block, Memory<byte> result)
            {
                lock (_savingLocker)
                {
                    innerResultHandler(block, result);
                }

                _notificationClient.TryNotify(block, totalBlocks);

                SignalAsCompleted();

                lock (_rentedBuffers)
                {
                    ArrayPool<byte>.Shared.Return(_rentedBuffers[block.Id]);
                    _rentedBuffers.Remove(block.Id);
                }
            }
        }

        private void HandleError(Exception e)
        {
            _pool.RequestCancellation();
            lock (_exceptions)
            {
                _exceptions.Add(e);
            }

            SetAsFailed();
        }

        private void SetupCompletionEvent(int totalBlocks) => _onFinishedEvent.Reset(totalBlocks);

        private void SignalAsCompleted() => _onFinishedEvent.Signal();

        private void SetAsFailed()
        {
            _isFailed = true;
            _onFinishedEvent.Reset(0);
        }

        private void WaitForCompletion()
        {
            _onFinishedEvent.Wait();
            _pool.Wait();
        }

        public void Dispose()
        {
            _onFinishedEvent?.Dispose();
        }
    }
}