using System;
using System.Threading.Tasks;

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
        Task SetRequestBizStepAsync(string bizStep);
        /// <summary>
        /// Report about error and set request completed
        /// </summary>
        /// <param name="techMessage">technical message</param>
        /// <param name="userFriendlyMessage">message for user</param>
        Task CompleteProcessingWithErrorAsync(string techMessage, string userFriendlyMessage = null);
        /// <summary>
        /// Report about error and set request completed
        /// </summary>
        /// <param name="userFriendlyMessage">message for user</param>
        /// <param name="e">occurred exception</param>
        Task CompleteProcessingWithErrorAsync(string userFriendlyMessage, Exception e);
        /// <summary>
        /// Report about error and set request completed
        /// </summary>
        /// <param name="e">occurred exception</param>
        Task CompleteProcessingWithErrorAsync(Exception e);
        /// <summary>
        /// Save result object and set request completed
        /// </summary>
        Task CompleteProcessingWithResultAsync(object objectResult);
        /// <summary>
        /// Save binary result and set request completed
        /// </summary>
        Task CompleteProcessingWithResultAsync(byte[] binaryResult);
        /// <summary>
        /// Set request completed without any result content
        /// </summary>
        Task CompleteProcessingAsync();
    }
}