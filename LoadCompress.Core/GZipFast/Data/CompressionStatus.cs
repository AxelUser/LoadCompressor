namespace LoadCompress.Core.GZipFast.Data
{
    public class CompressionStatus
    {
        public CompressionStatus(long totalBytesProceeded, int proceededBlocks, int blocksInTotal)
        {
            TotalBytesProceeded = totalBytesProceeded;
            ProceededBlocks = proceededBlocks;
            BlocksInTotal = blocksInTotal;
        }

        public long TotalBytesProceeded { get; }

        public int ProceededBlocks { get; }

        public int BlocksInTotal { get; }
    }
}