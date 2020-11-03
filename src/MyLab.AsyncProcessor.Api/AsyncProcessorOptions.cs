using System;

namespace MyLab.AsyncProcessor.Api
{
    /// <summary>
    /// Contains options
    /// </summary>
    public class AsyncProcessorOptions
    {
        /// <summary>
        /// Defines a prefix for redis key names that correspond to requests
        /// </summary>
        public string RedisKeyPrefix { get; set; }

        /// <summary>
        /// When that time is elapsed and was no event then request will be deleted 
        /// </summary>
        public TimeSpan MaxIdleTime { get; set; }

        /// <summary>
        /// When that time is elapsed after request completed then request will be deleted
        /// </summary>
        public TimeSpan MaxStoreTime { get; set; }

        /// <summary>
        /// Gets queue exchange name to publish requests
        /// </summary>
        public string QueueExchange { get; set; }
    }
}
