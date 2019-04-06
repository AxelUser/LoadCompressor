using System;

namespace LoadCompress.Cli
{
    public class CommandParsingException: Exception
    {
        public CommandParsingException(string message) : base(message)
        {
        }
    }
}