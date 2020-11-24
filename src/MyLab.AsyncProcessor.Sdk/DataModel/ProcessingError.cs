using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.DataModel
{
    /// <summary>
    /// Contains processing error details
    /// </summary>
    public class ProcessingError
    {
        /// <summary>
        /// User friendly business level message
        /// </summary>
        [JsonProperty("bizMgs")]
        public string BizMessage { get; set; }

        /// <summary>
        /// Technical level message. e.g. exception message.
        /// </summary>
        [JsonProperty("techMgs")]
        public string TechMessage { get; set; }

        /// <summary>
        /// Technical level description. e.g. exception stack trace.
        /// </summary>
        [JsonProperty("techInfo")]
        public string TechInfo { get; set; }

        /// <summary>
        /// Literal error identifier
        /// </summary>
        [JsonProperty("errorId")]
        public string ErrorId { get; set; }
    }
}