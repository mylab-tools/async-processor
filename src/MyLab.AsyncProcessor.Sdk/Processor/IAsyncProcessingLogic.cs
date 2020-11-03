using System.Threading.Tasks;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    interface IAsyncProcessingLogic<T>
    {
        Task Process(T request, IProcessingOperator op);
    }
}