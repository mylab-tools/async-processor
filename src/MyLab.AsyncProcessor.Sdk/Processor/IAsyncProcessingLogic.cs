using System.Threading.Tasks;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Performs request processing
    /// </summary>
    /// <typeparam name="T">Request type</typeparam>
    public interface IAsyncProcessingLogic<in T>
    {
        /// <summary>
        /// Processes request
        /// </summary>
        Task ProcessAsync(T request, IProcessingOperator op);
    }
}