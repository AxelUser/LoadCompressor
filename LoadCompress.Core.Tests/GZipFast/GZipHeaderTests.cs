using System;
using System.IO;
using FluentAssertions;
using LoadCompress.Core.GZipFast.Data;
using NUnit.Framework;

namespace LoadCompress.Core.Tests.GZipFast
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class GZipHeaderTests
    {
        [Test]
        public void ToBytes_should_return_minimal_size_if_has_no_blocks()
        {
            Span<byte> buffer = new byte[GZipHeader.MinimalSize];
            var header = new GZipHeader(new GZipBlock[0], long.MaxValue);

            var size = header.ToBytes(buffer);
            size.Should().Be(GZipHeader.MinimalSize).And.Be(header.GetSize());
        }

        [Test]
        public void Read_should_return_valid_header()
        {
            var header = new GZipHeader(new []
            {
                new GZipBlock(int.MaxValue, int.MaxValue, long.MaxValue),
                new GZipBlock(int.MaxValue, int.MaxValue, long.MaxValue),
                new GZipBlock(int.MaxValue, int.MaxValue, long.MaxValue),
                new GZipBlock(int.MaxValue, int.MaxValue, long.MaxValue)
            }, long.MaxValue);
            Span<byte> buffer = new byte[header.GetSize()];

            var size = header.ToBytes(buffer);

            size.Should().Be(header.GetSize());

            var resultedHeader = GZipHeader.Read(new MemoryStream(buffer.ToArray()));
            resultedHeader.BlocksCount.Should().Be(header.BlocksCount);

            for (var i = 0; i < resultedHeader.BlocksCount; i++)
            {
                resultedHeader[i].Should().Be(header[i], "Elements at index {0} must be equal", i);
            }
        }
    }
}