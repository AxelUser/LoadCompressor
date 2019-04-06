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
3. Compressed and decompressed file will be saved at `CompressionResults` (`bin` fodler).

### Via CLI
1. Run `dotnet LoadCompress.Cli.dll compress <source file path> <destination file path>` to compress file.
2. Run `dotnet LoadCompress.Cli.dll decompress <source file path> <destination file path>` to decompress the same file.