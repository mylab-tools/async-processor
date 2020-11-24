using System;

namespace MyLab.AsyncProcessor.Api
{
    /// <summary>
    /// Contains options
    /// </summary>
    public class AsyncProcessorOptions
    {
        /// <summary>
        /// Defines a prefix for redis key names that correspond to requests. REQUIRED.
        /// </summary>
        public string RedisKeyPrefix { get; set; }

        /// <summary>
        /// When that time is elapsed and was no event then request will be deleted . REQUIRED.
        /// </summary>
        public TimeSpan MaxIdleTime { get; set; }

        /// <summary>
        /// When that time is elapsed after request completed then request will be deleted. REQUIRED.
        /// </summary>
        public TimeSpan MaxStoreTime { get; set; }

        /// <summary>
        /// Gets queue exchange name to publish requests. OPTIONAL.
        /// </summary>
        public string QueueExchange { get; set; }

        /// <summary>
        /// Gets queue routing key to publish requests. OPTIONAL.
        /// </summary>
        public string QueueRoutingKey { get; set; }

        /// <summary>
        /// Dead letter queue. OPTIONAL.
        /// </summary>
        public string DeadLetter { get; set; }

        /// <summary>
        /// Callback exchange name. OPTIONAL.
        /// </summary>
        public string Callback { get; set; }
    }
}
