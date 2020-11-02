using MyLab.AsyncProcessor.Api.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyLab.AsyncProcessor.Api.DataModel
{
    public class CreateRequest
    {
        [JsonProperty("routing")]
        public string Routing { get; set; }

        [JsonProperty("data")]
        [JsonConverter(typeof(CustomDataConverter))]
        public string Data { get; set; }
    }
}