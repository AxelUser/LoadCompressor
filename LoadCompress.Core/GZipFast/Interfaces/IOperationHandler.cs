using System;
using System.IO;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Queues;

namespace LoadCompress.Core.GZipFast.Interfaces
{
    internal interface IOperationHandler
    {
        IGZipOperationQueue CreateQueue(GZipTransformationQueue transformationQueue);

        int GetBlockSize(GZipHeader header, int blockIndex);

        GZipBlock GetBlock(GZipHeader header, int blockIndex, int bytesRead);

        ResultHandler CreateResultHandler(Stream destination, GZipHeader header);

        void OnCompletedSuccessfully(Stream dest, GZipHeader header);
    }
}