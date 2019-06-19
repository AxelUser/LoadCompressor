using LoadCompress.Core.GZipFast.Interfaces;

namespace LoadCompress.Core.GZipFast.Handlers
{
    internal interface IHandlerFactory
    {
        IOperationHandler Create();
    }
}