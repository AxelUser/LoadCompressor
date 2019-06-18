using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Interfaces;
using LoadCompress.Core.GZipFast.Queues;
using LoadCompress.Core.Notification;

namespace LoadCompress.Core.GZipFast.Runners
{
    internal class GZipCompressionRunner: IGZipOperationRunner, IDisposable
    {
        private readonly IQueueFactory _queueFactory;
        private readonly IProgressNotificationClient _notificationClient;
        private readonly GZipTransformationQueue _transformationQueue;
        private readonly List<Exception> _exceptions;
        private readonly CountdownEvent _onFinishedEvent;
        private readonly WorkersPool _pool;

        private readonly Dictionary<int, byte[]> _rentedBuffers;

        private volatile bool _isFailed;
        private readonly object _savingLocker = new object();

        internal GZipCompressionRunner(IQueueFactory queueFactory, IProgressNotificationClient notificationClient, WorkersPool pool)
        {
            _rentedBuffers  = new Dictionary<int, byte[]>();
            _queueFactory = queueFactory;
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
            _isFailed = false;
            var totalBlocks = header.BlocksCount;
            var queue = _queueFactory.CreateCompressionQueue(_transformationQueue);

            var completedBlocks = new List<GZipBlock>();

            source.Seek(0, SeekOrigin.Begin);
            destination.Seek(header.GetSize(), SeekOrigin.Begin);

            SetupCompletionEvent(header.BlocksCount);

            for (var i = 0; i < header.BlocksCount; i++)
            {
                if(_isFailed)
                    return;
                var sourceBuffer = ArrayPool<byte>.Shared.Rent((int) header.BlockSize);

                var bytesRead = source.Read(sourceBuffer.AsSpan(0, (int)header.BlockSize));

                var block = new GZipBlock(i, 0, bytesRead);
                lock (_rentedBuffers)
                {
                    _rentedBuffers[block.Id] = sourceBuffer;
                }
                queue.Enqueue(block, sourceBuffer.AsMemory(0, bytesRead), HandleResult);
            }

            WaitForCompletion();

            if (_exceptions.Count > 0)
                throw new AggregateException("Failed to compress", _exceptions);

            header.MergeBlocks(completedBlocks);

            WriteHeader(header, destination);

            void HandleResult(GZipBlock block, Memory<byte> result)
            {
                lock (_savingLocker)
                {
                    completedBlocks.Add(block);
                    destination.Write(result.Span);
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

        private void WriteHeader(GZipHeader header, Stream destination)
        {
            var headerSize = header.GetSize();

            destination.Seek(0, SeekOrigin.Begin);
            byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(headerSize);

            header.ToBytes(headerBuffer.AsSpan(0, headerSize));
            destination.Write(headerBuffer.AsSpan(0, headerSize));

            ArrayPool<byte>.Shared.Return(headerBuffer);
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