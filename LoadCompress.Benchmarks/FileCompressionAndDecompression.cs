using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using LoadCompress.Core;
using LoadCompress.Core.GZipFast;

namespace LoadCompress.Benchmarks
{
    [MemoryDiagnoser]
    public class FileCompressionAndDecompression
    {
        [Params(1_000, 1_000_000, 10_000_000)]
        public int WordsCount;

        private int _sourceFileBytesCount;
        private FileStream _sourceStream;
        private Guid _fileId;
        private FileStream _destStream;
        private FileStream _validationStream;
        private string _dir;

        [GlobalSetup]
        public void SetUp()
        {
            _fileId = Guid.NewGuid();
            var sourcePath = $"{_fileId}.source.txt";
            _sourceFileBytesCount = TestFilesGenerator.Generate(sourcePath, WordsCount);
            _sourceStream = File.OpenRead(sourcePath);
        }

        [IterationSetup]
        public void IterationSetUp()
        {
            _dir = Guid.NewGuid().ToString();
            Directory.CreateDirectory(_dir);

            var destPath = Path.Combine(_dir, $"{_fileId}.compressed.hex");
            _destStream = File.Create(destPath);

            var validationPath = Path.Combine(_dir, $"{_fileId}.validation.txt");
            _validationStream = File.Create(validationPath);
        }

        [IterationCleanup]
        public void IterationCleanUp()
        {
            _destStream.Close();
            _validationStream.Close();
            Directory.Delete(_dir, true);
        }

        [Benchmark]
        public void RunFull()
        {
            using (var processor = new GZipCompressor())
            {
                processor.Compress(_sourceStream, _destStream, _sourceFileBytesCount, 1.Mb());
                _destStream.Flush();
                processor.Decompress(_destStream, _validationStream);
                _validationStream.Flush();
            }
        }

        [GlobalCleanup]
        public void CleanUp()
        {

        }
    }
}