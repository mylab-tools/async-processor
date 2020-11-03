using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.DataModel
{
    public class CreateRequest
    {
        [JsonProperty("routing")]
        public string Routing { get; set; }

        [JsonProperty("content")]
        [JsonConverter(typeof(CustomDataConverter))]
        public string Content { get; set; }
    }
}