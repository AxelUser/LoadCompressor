using System;
using System.IO;
using System.IO.Compression;

namespace LoadCompress.Core.GZipFast
{
    public static class GZipHelper
    {
        public static int CompressBytes(ReadOnlyMemory<byte> buffer, MemoryStream dest)
        {
            using (var gzipCompress = new GZipStream(dest, CompressionLevel.Optimal, true))
            {
                gzipCompress.Write(buffer.Span);                
            }

            return (int)dest.Length;
        }

        public static int DecompressBytes(MemoryStream compressedStream, Span<byte> decompressedBuffer)
        {
            using (var gzipDecompress = new GZipStream(compressedStream, CompressionMode.Decompress, true))
            {
                return gzipDecompress.Read(decompressedBuffer);
            }
        }
    }
}