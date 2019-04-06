using System;
using System.IO;
using LoadCompress.Core;
using LoadCompress.Core.GZipFast;
using LoadCompress.Core.GZipFast.Data;

namespace LoadCompress.Cli
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var cmd = CommandParser.Parse(args);
                
                switch (cmd.OperationType)
                {
                    case OperationType.Compression:
                        ProceedCompression(cmd);
                        break;
                    case OperationType.Decompression:
                        ProceedDecompression(cmd);
                        break;
                }

            }
            catch (CommandParsingException commandException)
            {
                Console.WriteLine($"{commandException.Message}\n\nExample: {CommandParser.FormatHelp}");
                throw;
            }
        }

        private static void ProceedCompression(Command cmd)
        {
            var fileSize = new FileInfo(cmd.InputFilePath).Length;

            using (var compressor = new GZipCompressor())
            using (var input = File.OpenRead(cmd.InputFilePath))
            using (var output = File.OpenWrite(cmd.OutputFilePath))
            {
                if (!cmd.IsSilent)
                    compressor.ProgressUpdate += CompressorOnProgressUpdate;

                Console.WriteLine("Block size is 1Mb");
                compressor.Compress(input, output, fileSize, 1.Mb());

                if (!cmd.IsSilent)
                    compressor.ProgressUpdate -= CompressorOnProgressUpdate;
            }
        }

        private static void ProceedDecompression(Command cmd)
        {
            using (var compressor = new GZipCompressor())
            using (var input = File.OpenRead(cmd.InputFilePath))
            using (var output = File.OpenWrite(cmd.OutputFilePath))
            {
                if(!cmd.IsSilent)
                    compressor.ProgressUpdate += CompressorOnProgressUpdate;

                Console.WriteLine("Block size is 1Mb");
                compressor.Decompress(input, output);

                if (!cmd.IsSilent)
                    compressor.ProgressUpdate -= CompressorOnProgressUpdate;
            }
        }

        private static void CompressorOnProgressUpdate(object sender, CompressionStatus e)
        {
            Console.WriteLine($"{e.ProceededBlocks}/{e.BlocksInTotal} ({e.TotalBytesProceeded} bytes)");
        }
    }
}
