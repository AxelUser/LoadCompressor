using BenchmarkDotNet.Running;

namespace LoadCompress.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<FileCompressionAndDecompression>();
        }
    }
}
