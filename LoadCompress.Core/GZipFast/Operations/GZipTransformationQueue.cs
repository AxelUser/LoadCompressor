using System;
using System.Buffers;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Interfaces;

namespace LoadCompress.Core.GZipFast.Operations
{
    /// <summary>
    /// Delegate for compression/decompression.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="outputBuffer"></param>
    /// <returns>Length of output data</returns>
    internal delegate int Transformation(GZipBlock block, Memory<byte> source, Memory<byte> outputBuffer);

    /// <summary>
    /// Common task queue for performing bytes transformation.
    /// </summary>
    internal class GZipTransformationQueue
    {
        private readonly WorkersPool _pool;
        private readonly Action<Exception> _onError;

        internal GZipTransformationQueue(WorkersPool pool, Action<Exception> onError)
        {
            _pool = pool;
            _onError = onError;
        }

        /// <summary>
        /// Enqueue transformation task.
        /// </summary>
        /// <param name="pendingBlock">Block which is transformed</param>
        /// <param name="sourceBuffer">Source bytes</param>
        /// <param name="sourceTransformation">Delegate for bytes transformation</param>
        /// <param name="resultHandler">Delegate to handle transformation results</param>
        /// <param name="minBufferSize">Minimal expected length of result</param>
        /// <param name="operationType">Compression/Decompression (used just for logging)</param>
        internal void Enqueue(GZipBlock pendingBlock,
            Memory<byte> sourceBuffer,
            Transformation sourceTransformation,
            ResultHandler resultHandler,
            int minBufferSize,            
            OperationType operationType)
        {
            var isScheduled = _pool.TryScheduleWork(Handle);
            if (!isScheduled)
                _onError(new InvalidOperationException("Operation is cancelled"));

            void Handle()
            {
                try
                {
                    // TODO maybe memory-inefficient, but it will be reused.
                    byte[] resultBufferArray = ArrayPool<byte>.Shared.Rent(minBufferSize);

                    var resultLength = sourceTransformation(pendingBlock, sourceBuffer, resultBufferArray);

                    resultHandler(pendingBlock, resultBufferArray.AsMemory(0, resultLength));

                    ArrayPool<byte>.Shared.Return(resultBufferArray);
                }
                catch (Exception e)
                {
                    _onError(new Exception($"{operationType} failed for block {pendingBlock}", e));
                }
            }
        }
    }
}