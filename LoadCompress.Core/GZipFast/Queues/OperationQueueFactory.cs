using LoadCompress.Core.GZipFast.Interfaces;

namespace LoadCompress.Core.GZipFast.Queues
{
    internal class OperationQueueFactory: IQueueFactory
    {
        public IGZipOperationQueue CreateCompressionQueue(GZipTransformationQueue transformationQueue) => new GZipCompressionQueue(transformationQueue);

        public IGZipOperationQueue CreateDecompressionQueue(GZipTransformationQueue transformationQueue) => new GZipDecompressionQueue(transformationQueue);
    }
}