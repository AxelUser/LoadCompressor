using System;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Interfaces;
using Microsoft.IO;

namespace LoadCompress.Core.GZipFast.Queues
{
    internal class GZipCompressionQueue: IGZipOperationQueue
    {
        private readonly GZipTransformationQueue _transformationQueue;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

        public GZipCompressionQueue(GZipTransformationQueue transformationQueue)
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
            _transformationQueue.Enqueue(pendingBlock, sourceBuffer, CompressionOperation, resultHandler, minOutputSize, OperationType.Compression);

        }

        private int CompressionOperation(GZipBlock block, Memory<byte> source, Memory<byte> outputBuffer)
        {
            using (var compressedStream = _memoryStreamManager.GetStream())
            {
                var compressedSize = GZipHelper.CompressBytes(source, compressedStream);
                block.Size = compressedSize;

                compressedStream.Position = 0;
                compressedStream.Read(outputBuffer.Span);

                return compressedSize;
            }
        }
    }
}