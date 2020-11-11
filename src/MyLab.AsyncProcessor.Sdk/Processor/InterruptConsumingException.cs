using System;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Throw to interrupt message consuming
    /// </summary>
    public class InterruptConsumingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InterruptConsumingException"/>
        /// </summary>
        public InterruptConsumingException() : base("There was force consuming interruption")
        {
            
        }
    }
}
