using LoadCompress.Core.GZipFast.Data;

namespace LoadCompress.Core.Notification
{
    internal interface IProgressNotificationClient
    {
        bool TryNotify(GZipBlock block, int totalBlocks);
    }
}