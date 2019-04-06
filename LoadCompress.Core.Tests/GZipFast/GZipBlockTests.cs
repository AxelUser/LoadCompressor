using System;
using FluentAssertions;
using LoadCompress.Core.GZipFast.Data;
using NUnit.Framework;

namespace LoadCompress.Core.Tests.GZipFast
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class GZipBlockTests
    {
        [Test]
        public void ToBytes_should_return_correct_size()
        {
            var block = new GZipBlock(int.MaxValue, int.MaxValue, long.MaxValue);
            var expectedSize = GZipBlock.SelfSize;

            Span<byte> buffer = new byte[expectedSize];

            block.ToBytes(buffer).Should().Be(expectedSize);
        }

        [Test]
        public void ToBytes_fail_if_buffer_size_too_small()
        {
            var block = new GZipBlock(int.MaxValue, int.MaxValue, long.MaxValue);
            var expectedSize = GZipBlock.SelfSize;

            block.Invoking(zipBlock =>
            {
                Span<byte> buffer = new byte[expectedSize - 1];
                zipBlock.ToBytes(buffer);
            }).Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void FromBytes_should_return_same_block()
        {
            var block = new GZipBlock(int.MaxValue, int.MaxValue, long.MaxValue);
            var expectedSize = GZipBlock.SelfSize;

            Span<byte> buffer = new byte[expectedSize];

            block.ToBytes(buffer).Should().Be(expectedSize);

            GZipBlock.FromBytes(buffer).Should().Be(block);
        }
    }
}
