using LoadCompress.Core;

namespace LoadCompress.Cli
{
    public class Command
    {
        public Command(OperationType operationType, string inputFilePath, string outputFilePath, bool isSilent)
        {
            OperationType = operationType;
            InputFilePath = inputFilePath;
            OutputFilePath = outputFilePath;
            IsSilent = isSilent;
        }

        public OperationType OperationType { get; }
        public string InputFilePath { get; }
        public string OutputFilePath { get; }
        public bool IsSilent { get; }
    }
}