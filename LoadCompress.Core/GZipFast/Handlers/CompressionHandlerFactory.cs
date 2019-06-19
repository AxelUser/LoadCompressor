using LoadCompress.Core.GZipFast.Interfaces;

namespace LoadCompress.Core.GZipFast.Handlers
{
    internal class CompressionHandlerFactory: IHandlerFactory
    {
        private readonly IQueueFactory _queueFactory;

        public CompressionHandlerFactory(IQueueFactory queueFactory)
        {
            _queueFactory = queueFactory;
        }

        public IOperationHandler Create()
        {
            return new CompressionHandler(_queueFactory);
        }
    }
}