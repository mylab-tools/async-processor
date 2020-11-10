using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MyLab.AsyncProcessor.Sdk.DataModel
{
    public class RequestStatus
    {
        [JsonProperty("processStep")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ProcessStep Step { get; set; }

        [JsonProperty("bizStep")]
        public string BizStep { get; set; }

        [JsonProperty("successful")]
        public bool Successful { get; set; }

        [JsonProperty("error")]
        public ProcessingError Error { get; set; }

        [JsonProperty("resultSize")]
        public long ResponseSize{ get; set; }

        [JsonProperty("resultMime")]
        public string ResponseMimeType { get; set; }
    }
}
