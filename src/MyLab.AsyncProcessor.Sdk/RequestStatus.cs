using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk
{
    public class RequestStatus
    {
        [JsonProperty("processStep")]
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
