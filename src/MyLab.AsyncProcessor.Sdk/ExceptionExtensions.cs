using System;
using MyLab.AsyncProcessor.Sdk.DataModel;

namespace MyLab.AsyncProcessor.Sdk
{
    /// <summary>
    /// Extensions for <see cref="Exception"/>
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Converts an exception to <see cref="ProcessingError"/>
        /// </summary>
        public static ProcessingError ToProcessingError(this Exception e, string errorId = null, string bizMsg = null)
        {
            return new ProcessingError
            {
                TechInfo = e.ToString(),
                TechMessage = e.Message,
                BizMessage = bizMsg,
                ErrorId = errorId
            };
        }
    }
}