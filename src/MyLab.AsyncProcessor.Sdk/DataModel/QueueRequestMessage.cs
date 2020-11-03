using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.DataModel
{
    /// <summary>
    /// Describes queue message that contains request details
    /// </summary>
    public class QueueRequestMessage
    {
        /// <summary>
        /// Request identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        /// <summary>
        /// Request content
        /// </summary>
        [JsonProperty("content")]
        [JsonConverter(typeof(CustomDataConverter))]
        public string Content { get; set; }
    }
}
