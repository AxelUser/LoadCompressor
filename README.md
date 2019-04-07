# LoadCompressor

## Projects
- `LoadCompress.Core`: logic for multithreaded compression.
- `LoadCompress.Cli`: console interface for compressor.
- `LoadCompress.Core.Tests`: unit-tests for internal logic.
- `LoadCompress.Benchmarks`: main benchmark that is used for measuring perfomance.
- `LoadCompress.IntegrationTests`: intergation test, that run CLI.

## How to run

### Via integration test
1. Put test data into `LoadCompress.IntegrationTests/TestData`.
2. Run `ApprovalTest`.
3. Compressed and decompressed files will be saved at `CompressionResults` (`bin` fodler).

### Via CLI
1. Run `dotnet LoadCompress.Cli.dll compress <source file path> <destination file path>` to compress file.
2. Run `dotnet LoadCompress.Cli.dll decompress <source file path> <destination file path>` to decompress the same file.

## Perfomace
```
// * Summary *

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17134.648 (1803/April2018Update/Redstone4)
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.101
  [Host]     : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT
  Job-QCSLHH : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT

InvocationCount=1  UnrollFactor=1

|  Method | WordsCount |       Mean |      Error |     StdDev |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|-------- |----------- |-----------:|-----------:|-----------:|----------:|----------:|----------:|----------:|
| RunFull |       1000 |   4.581 ms |  0.2690 ms |  0.7890 ms |         - |         - |         - |   1.02 MB |
| RunFull |    1000000 | 104.201 ms |  2.0804 ms |  4.9038 ms | 1000.0000 | 1000.0000 | 1000.0000 |   5.54 MB |
| RunFull |   10000000 | 679.365 ms | 13.4759 ms | 31.4995 ms | 2000.0000 | 2000.0000 | 2000.0000 |  11.57 MB |
```