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
        /// Period for request processing else request will be deleted.
        /// </summary>
        public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(1);
        
        /// <summary>
        /// Request lifetime after processing.
        /// </summary>
        public TimeSpan RestTimeout { get; set; } = TimeSpan.FromMinutes(10);

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
