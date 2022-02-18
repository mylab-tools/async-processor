namespace MyLab.AsyncProcessor.Api
{
    /// <summary>
    /// Represent AsyncProc specified OTLP options
    /// </summary>
    public class AsyncProcOtlpOptions
    {
        /// <summary>
        /// Enabled OTLP
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Name of current service in otlp outputs
        /// </summary>
        public string ServiceName { get; set; }
    }
}
