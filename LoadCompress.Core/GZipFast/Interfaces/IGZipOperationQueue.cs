using System;
using LoadCompress.Core.GZipFast.Data;

namespace LoadCompress.Core.GZipFast.Interfaces
{
    internal interface IGZipOperationQueue
    {
        /// <summary>
        /// Add task into operation queue.
        /// </summary>
        /// <param name="pendingBlock">Block for transformation</param>
        /// <param name="sourceBuffer">Bytes from source</param>
        /// <param name="resultHandler">Delegate which receives transformation result</param>
        void Enqueue(GZipBlock pendingBlock,
            Memory<byte> sourceBuffer,
            ResultHandler resultHandler);
    }
}