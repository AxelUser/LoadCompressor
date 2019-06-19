using LoadCompress.Core.GZipFast.Interfaces;

namespace LoadCompress.Core.GZipFast.Handlers
{
    internal class DecompressionHandlerFactory: IHandlerFactory
    {
        private readonly IQueueFactory _queueFactory;

        public DecompressionHandlerFactory(IQueueFactory queueFactory)
        {
            _queueFactory = queueFactory;
        }

        public IOperationHandler Create()
        {
            return new DecompressionHandler(_queueFactory);
        }
    }
}