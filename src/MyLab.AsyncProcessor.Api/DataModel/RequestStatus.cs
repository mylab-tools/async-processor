using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Api.DataModel
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
    }
}
