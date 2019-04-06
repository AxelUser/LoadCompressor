using System.IO;
using LoadCompress.Core;

namespace LoadCompress.Cli
{
    public static class CommandParser
    {
        public const string FormatHelp = "compress|decompress <source> <destination>";

        public static Command Parse(string[] args)
        {
            if(args.Length < 3)
                throw new CommandParsingException("Should have at least 3 arguments");

            return new Command(args.GetOperation(),
                args.GetSourceFullPath(),
                args.GetDestinationFullPath(),
                args.GetSilenceFlag());
        }

        private static OperationType GetOperation(this string[] args)
        {
            switch (args[0].ToLower())
            {
                case "compress":
                    return OperationType.Compression;
                case "decompress":
                    return OperationType.Decompression;
                default:
                    throw new CommandParsingException($"Does not support operation {args[0]}");
            }
        }

        private static string GetSourceFullPath(this string[] args)
        {
            var fileInfo = new FileInfo(args[1]);
            if (!fileInfo.Exists)
                throw new CommandParsingException($"File '{args[1]}' does not exist");

            return fileInfo.FullName;
        }

        private static string GetDestinationFullPath(this string[] args)
        {
            var fileInfo = new FileInfo(args[2]);
            return fileInfo.FullName;
        }

        private static bool GetSilenceFlag(this string[] args, bool fallback = false)
        {
            if (args.Length == 4 && args[3].ToLower() == "-s")
                return true;
            return fallback;
        }
    }
}