using System;
using System.IO;
using LoadCompress.Core.GZipFast.Data;

namespace LoadCompress.Core.GZipFast.Interfaces
{
    internal delegate void ResultHandler(GZipBlock block, Memory<byte> transformedData);

    internal interface IGZipOperationRunner
    {
        /// <summary>
        /// Start operation and block thread until it's completed or failed.
        /// </summary>
        /// <param name="header">Header with metadata</param>
        /// <param name="source">Input stream</param>
        /// <param name="destination">Output stream</param>
        void RunForCompletion(GZipHeader header, Stream source, Stream destination);
    }
}