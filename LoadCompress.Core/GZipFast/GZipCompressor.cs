using System;
using System.IO;
using System.Threading;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Handlers;
using LoadCompress.Core.GZipFast.Interfaces;
using LoadCompress.Core.GZipFast.Queues;
using LoadCompress.Core.GZipFast.Runners;
using LoadCompress.Core.Notification;

namespace LoadCompress.Core.GZipFast
{
    public class GZipCompressor: IDisposable
    {
        private readonly CountdownEvent _onFinishedEvent;
        private readonly IQueueFactory _queueFactory;
        public event EventHandler<CompressionStatus> ProgressUpdate;

        public GZipCompressor()
        {
            _onFinishedEvent = new CountdownEvent(0);
            _queueFactory = new OperationQueueFactory();
        }

        public void Compress(Stream source, Stream dest, long sourceSize, int blockSize)
        {
            if (!dest.CanSeek || !source.CanSeek)
                throw new InvalidOperationException("Non-seekable streams are not supported");

            var blocksCount = (int)(sourceSize / blockSize + (sourceSize % blockSize == 0 ? 0 : 1));

            using (var notifier = CreateAndStartNotification())
            using (var pool = new WorkersPool())
            using (var runner = new GZipOperationRunner(new CompressionHandlerFactory(_queueFactory), notifier, pool))
            {
                var header = GZipHeader.CreateEmpty(blocksCount, blockSize);
                source.Seek(0, SeekOrigin.Begin);
                dest.Seek(header.GetSize(), SeekOrigin.Begin);

                runner.RunForCompletion(header, source, dest);
            }
        }

        public void Decompress(Stream source, Stream dest)
        {
            if (!dest.CanSeek || !source.CanSeek)
                throw new InvalidOperationException("Non-seekable streams are not supported");

            source.Seek(0, SeekOrigin.Begin);
            var header = GZipHeader.Read(source);

            using (var notifier = CreateAndStartNotification())
            using (var pool = new WorkersPool())
            using (var runner = new GZipOperationRunner(new DecompressionHandlerFactory(_queueFactory), notifier, pool))
            {
                dest.Seek(0, SeekOrigin.Begin);
                runner.RunForCompletion(header, source, dest);
            }
        }

        public void Dispose()
        {
            _onFinishedEvent?.Dispose();
        }

        protected virtual void OnProgressUpdate(CompressionStatus e)
        {
            ProgressUpdate?.Invoke(this, e);
        }

        private ProgressNotifier CreateAndStartNotification() =>
            new ProgressNotifier(OnProgressUpdate, block => block.OriginalSize, true);
    }
}
