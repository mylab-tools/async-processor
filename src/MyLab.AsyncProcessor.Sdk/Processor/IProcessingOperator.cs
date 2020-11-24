using System;
using System.Threading.Tasks;
using MyLab.AsyncProcessor.Sdk.DataModel;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Provides abilities for processing reporting
    /// </summary>
    public interface IProcessingOperator
    {
        /// <summary>
        /// Sets business level step fro request
        /// </summary>
        Task SetBizStepAsync(string bizStep);
        /// <summary>
        /// Report about error and set request completed
        /// </summary>
        Task CompleteWithErrorAsync(ProcessingError error);
        /// <summary>
        /// Save result object and set request completed
        /// </summary>
        Task CompleteWithResultAsync(object objectResult);
        /// <summary>
        /// Save binary result and set request completed
        /// </summary>
        Task CompleteWithResultAsync(byte[] binaryResult);
        /// <summary>
        /// Set request completed without any result content
        /// </summary>
        Task CompleteAsync();
    }
}