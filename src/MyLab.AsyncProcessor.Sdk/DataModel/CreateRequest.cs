using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.DataModel
{
    /// <summary>
    /// Contains request parameters
    /// </summary>
    public class CreateRequest
    {
        /// <summary>
        /// Defines predefined request id
        /// </summary>
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// Defines queue routing to send message to processor
        /// </summary>
        [JsonProperty("procRouting")]
        public string ProcRouting { get; set; }

        /// <summary>
        /// Defines queue routing to send callback messages through callback exchange
        /// </summary>
        [JsonProperty("callbackRouting")]
        public string CallbackRouting { get; set; }

        /// <summary>
        /// Request content
        /// </summary>
        [JsonProperty("content")]
        [JsonConverter(typeof(CustomDataConverter))]
        public string Content { get; set; }
    }
}