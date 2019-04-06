using System;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Interfaces;
using Microsoft.IO;

namespace LoadCompress.Core.GZipFast.Operations
{
    internal class GZipDecompressionQueue: IGZipOperationQueue
    {
        private readonly GZipTransformationQueue _transformationQueue;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public GZipDecompressionQueue(GZipTransformationQueue transformationQueue)
        {
            _transformationQueue = transformationQueue;
            _memoryStreamManager = new RecyclableMemoryStreamManager();
        }

        /// <inheritdoc />
        public void Enqueue(GZipBlock pendingBlock,
            Memory<byte> sourceBuffer,
            ResultHandler resultHandler)
        {
            var minOutputSize = (int)pendingBlock.OriginalSize * 2;
            _transformationQueue.Enqueue(pendingBlock, sourceBuffer, DecompressionOperation, resultHandler, minOutputSize, OperationType.Decompression);
        }

        private int DecompressionOperation(GZipBlock block, Memory<byte> source, Memory<byte> outputBuffer)
        {
            using (var compressedStream = _memoryStreamManager.GetStream())
            {
                compressedStream.Write(source.Span);
                compressedStream.Position = 0;

                var originalSize = (int)block.OriginalSize;

                GZipHelper.DecompressBytes(compressedStream, outputBuffer.Slice(0, originalSize).Span);

                return originalSize;
            }
        }
    }
}