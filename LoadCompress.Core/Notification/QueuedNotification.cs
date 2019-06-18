using LoadCompress.Core.GZipFast.Data;

namespace LoadCompress.Core.Notification
{
    internal class QueuedNotification
    {
        public GZipBlock Block { get; }

        public int TotalBlocks { get; }

        public QueuedNotification(GZipBlock block, int totalBlocks)
        {
            Block = block;
            TotalBlocks = totalBlocks;
        }
    }
}