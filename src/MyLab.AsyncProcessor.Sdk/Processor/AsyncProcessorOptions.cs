namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Contains AsyncProcessor config options
    /// </summary>
    public class AsyncProcessorOptions
    {
        /// <summary>
        /// Queue name which is source of request-messages
        /// </summary>
        public string Queue { get; set; }
    }
}