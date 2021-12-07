using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyLab.AsyncProcessor.Sdk.DataModel
{
    /// <summary>
    /// Contains request properties
    /// </summary>
    public class RequestDetails
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Current status
        /// </summary>
        [JsonProperty("status")]
        public RequestStatus Status { get; set; }

        /// <summary>
        /// Not null if processing result is ready and it is an object
        /// </summary>
        [JsonProperty("resObj")]
        public JToken ResultObject { get; set; }

        /// <summary>
        /// Not null if processing result is ready and it is a binary array
        /// </summary>
        [JsonProperty("resBin")]
        public byte[] ResultBin { get; set; }
    }
}