using System;
using System.IO;
using System.Threading;
using LoadCompress.Core.GZipFast.Data;
using LoadCompress.Core.GZipFast.Operations;

namespace LoadCompress.Core.GZipFast
{
    public class GZipCompressor: IDisposable
    {
        private readonly CountdownEvent _onFinishedEvent;
        public event EventHandler<CompressionStatus> ProgressUpdate;

        public GZipCompressor()
        {
            _onFinishedEvent = new CountdownEvent(0);
        }

        public void Compress(Stream source, Stream dest, long sourceSize, int blockSize)
        {
            var notifier = new ProgressNotifier(OnProgressUpdate, block => block.OriginalSize);
            var runner = new GZipCompressionRunner(notifier);

            if (!dest.CanSeek || !source.CanSeek)
                throw new InvalidOperationException("Non-seekable streams are not supported");

            var blocksCount = (int)(sourceSize / blockSize + (sourceSize % blockSize == 0 ? 0 : 1));

            Span<GZipBlock> blocks = new GZipBlock[blocksCount];
            var header = new GZipHeader(blocks, blockSize);

            runner.RunForCompletion(header, source, dest);
            runner.Dispose();
        }

        public void Decompress(Stream source, Stream dest)
        {
            var notifier = new ProgressNotifier(OnProgressUpdate, block => block.OriginalSize);
            var runner = new GZipDecompressionRunner(notifier);

            if (!dest.CanSeek || !source.CanSeek)
                throw new InvalidOperationException("Non-seekable streams are not supported");

            source.Seek(0, SeekOrigin.Begin);
            var header = GZipHeader.Read(source);

            runner.RunForCompletion(header, source, dest);
            runner.Dispose();
        }

        public void Dispose()
        {
            _onFinishedEvent?.Dispose();
        }

        protected virtual void OnProgressUpdate(CompressionStatus e)
        {
            ProgressUpdate?.Invoke(this, e);
        }
    }
}
