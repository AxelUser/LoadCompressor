using LoadCompress.Core.GZipFast.Queues;

namespace LoadCompress.Core.GZipFast.Interfaces
{
    internal interface IQueueFactory
    {
        IGZipOperationQueue CreateCompressionQueue(GZipTransformationQueue transformationQueue);
        IGZipOperationQueue CreateDecompressionQueue(GZipTransformationQueue transformationQueue);
    }
}