using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Interfaces;

namespace LoadCompress.Core.GZipFast.Operations
{
    internal class GZipDecompressionRunner: IGZipOperationRunner, IDisposable
    {
        private readonly ProgressNotifier _notifier;
        private readonly GZipTransformationQueue _transformationQueue;
        private readonly List<Exception> _exceptions;
        private readonly CountdownEvent _onFinishedEvent;
        private readonly WorkersPool _pool;
        private volatile bool _isFailed;

        private readonly object _savingLocker = new object();

        private readonly Dictionary<int, byte[]> _rentedBuffers;

        internal GZipDecompressionRunner(ProgressNotifier notifier)
        {
            _rentedBuffers = new Dictionary<int, byte[]>();
            _notifier = notifier;
            _pool = new WorkersPool();
            _transformationQueue = new GZipTransformationQueue(_pool, HandleError);
            _exceptions = new List<Exception>();
            _onFinishedEvent = new CountdownEvent(0);
            _isFailed = false;
        }

        /// <inheritdoc />
        public void RunForCompletion(GZipHeader header, Stream source, Stream destination)
        {
            _isFailed = false;
            var defaultBlockSize = header.BlockSize;
            var queue = new GZipDecompressionQueue(_transformationQueue);

            destination.Seek(0, SeekOrigin.Begin);

            SetupCompletionEvent(header.BlocksCount);

            _notifier.Start(header.BlocksCount);

            for (var i = 0; i < header.BlocksCount; i++)
            {
                if(_isFailed)
                    break;

                var sourceBuffer = ArrayPool<byte>.Shared.Rent(header[i].Size);

                var bytesRead = source.Read(sourceBuffer.AsSpan(0, header[i].Size));

                queue.Enqueue(header[i], sourceBuffer.AsMemory(0, bytesRead), HandleResult);

                lock (_rentedBuffers)
                {
                    _rentedBuffers[header[i].Id] = sourceBuffer;
                }
            }

            WaitForCompletion();
            _notifier.Stop();

            if (_exceptions.Count > 0)
                throw new AggregateException("Failed to decompress", _exceptions);

            void HandleResult(GZipBlock block, Memory<byte> result)
            {
                lock (_savingLocker)
                {
                    var offset = block.Id * defaultBlockSize;
                    destination.Seek(offset, SeekOrigin.Begin);
                    destination.Write(result.Span);
                }

                _notifier.TryNotify(block);

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
            _pool?.Dispose();
        }
    }
}