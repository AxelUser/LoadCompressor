using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Interfaces;
using LoadCompress.Core.GZipFast.Queues;

namespace LoadCompress.Core.GZipFast.Handlers
{
    internal class CompressionHandler: IOperationHandler
    {
        private readonly IQueueFactory _queueFactory;
        private readonly List<GZipBlock> _completedBlocks;

        public CompressionHandler(IQueueFactory queueFactory)
        {
            _queueFactory = queueFactory;
            _completedBlocks = new List<GZipBlock>();
        }

        public IGZipOperationQueue CreateQueue(GZipTransformationQueue transformationQueue)
        {
            return _queueFactory.CreateCompressionQueue(transformationQueue);
        }

        public int GetBlockSize(GZipHeader header, int blockIndex)
        {
            return (int)header.BlockSize;
        }

        public GZipBlock GetBlock(GZipHeader header, int blockIndex, int bytesRead)
        {
            return new GZipBlock(blockIndex, 0, bytesRead);
        }

        public ResultHandler CreateResultHandler(Stream destination, GZipHeader header)
        {
            return (block, result) =>
            {
                _completedBlocks.Add(block);
                destination.Write(result.Span);
            };
        }

        public void OnCompletedSuccessfully(Stream dest, GZipHeader header)
        {
            header.MergeBlocks(_completedBlocks);
            WriteHeader(header, dest);
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
    }
}