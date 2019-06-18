using System.IO;
using System.Threading;
using FluentAssertions;
using LoadCompress.Core.GZipFast;
using LoadCompress.Core.GZipFast.Data;
using NUnit.Framework;

namespace LoadCompress.Core.Tests.GZipFast
{
    [TestFixture(1_000_000, 10)]
    [Parallelizable]
    public class GZipCompressorTests
    {
        private readonly long _bytesCount;
        private readonly int _blockSize;
        private MemoryStream _source;
        private MemoryStream _compressed;
        private GZipCompressor _compressor;

        public GZipCompressorTests(long bytesCount, int blockSize)
        {
            _bytesCount = bytesCount;
            _blockSize = blockSize;
        }

        [SetUp]
        public void SetUp()
        {
            _source = Utils.GenerateBytes(_bytesCount);
            _compressed = new MemoryStream();
            _compressor = new GZipCompressor();
        }

        [TearDown]
        public void TearDown()
        {
            _compressor.Dispose();
        }


        [Test]
        public void Compress_should_work()
        {
            _compressor.Compress(_source, _compressed, _source.Length, _blockSize);
        }

        [Test]
        public void Decompress_should_return_original_bytes()
        {
            var sourceBytes = _source.ToArray();
            _compressor.Compress(_source, _compressed, _source.Length, _blockSize);
            
            var decompressed = new MemoryStream();

            _compressor.Decompress(_compressed, decompressed);
            var decompressedBytes = decompressed.ToArray();

            decompressedBytes.Should().HaveCount(sourceBytes.Length);
            decompressedBytes.Should().BeEquivalentTo(sourceBytes);
        }

        [Test]
        public void ProgressUpdate_should_return_all_notifications()
        {
            var counter = 0;
            var expectedBlocksCount = (int)(_bytesCount / _blockSize + (_bytesCount % _blockSize == 0 ? 0 : 1));

            _compressor.ProgressUpdate += ProgressUpdateHandler;
            _compressor.Compress(_source, _compressed, _source.Length, _blockSize);

            counter.Should().Be(expectedBlocksCount);
            counter = 0;

            var decompressed = new MemoryStream();
            _compressor.Decompress(_compressed, decompressed);

            counter.Should().Be(expectedBlocksCount);
            counter = 0;

            _compressor.ProgressUpdate -= ProgressUpdateHandler;

            void ProgressUpdateHandler(object sender, CompressionStatus e)
            {
                Interlocked.Increment(ref counter);
            }
        }
    }
}