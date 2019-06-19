using System.IO;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Interfaces;
using LoadCompress.Core.GZipFast.Queues;

namespace LoadCompress.Core.GZipFast.Handlers
{
    internal class DecompressionHandler: IOperationHandler
    {
        private readonly IQueueFactory _queueFactory;

        public DecompressionHandler(IQueueFactory queueFactory)
        {
            _queueFactory = queueFactory;
        }

        public IGZipOperationQueue CreateQueue(GZipTransformationQueue transformationQueue)
        {
            return _queueFactory.CreateDecompressionQueue(transformationQueue);
        }

        public int GetBlockSize(GZipHeader header, int blockIndex)
        {
            return header[blockIndex].Size;
        }

        public GZipBlock GetBlock(GZipHeader header, int blockIndex, int bytesRead)
        {
            return header[blockIndex];
        }

        public ResultHandler CreateResultHandler(Stream destination, GZipHeader header)
        {
            return (block, result) =>
            {
                var offset = block.Id * header.BlockSize;
                destination.Seek(offset, SeekOrigin.Begin);
                destination.Write(result.Span);
            };
        }

        public void OnCompletedSuccessfully(Stream dest, GZipHeader header)
        {
        }
    }
}