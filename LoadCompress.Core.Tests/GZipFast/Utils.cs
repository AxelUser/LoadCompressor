using System.IO;

namespace LoadCompress.Core.Tests.GZipFast
{
    public static class Utils
    {
        public static MemoryStream GenerateBytes(long bytesCount)
        {
            var source = new byte[bytesCount];
            for (var i = 0; i < bytesCount; i++)
            {
                source[i] = byte.MaxValue;
            }
            return new MemoryStream(source);
        }
    }
}