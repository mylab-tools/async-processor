using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.DataModel
{
    /// <summary>
    /// Contains info about request status changes
    /// </summary>
    public class ChangeStatusCallbackMessage
    {
        /// <summary>
        /// Request identifier
        /// </summary>
        [JsonProperty("reqId")]
        public string RequestId { get; set; }
        /// <summary>
        /// Not null if biz step changed
        /// </summary>
        [JsonProperty("newBizStep")]
        public string NewBizStep { get; set; }

        /// <summary>
        /// Not null if process step changed
        /// </summary>
        [JsonProperty("newProcStep")]
        public ProcessStep? NewProcessStep { get; set; }

        /// <summary>
        /// Not null if there was processing error
        /// </summary>
        [JsonProperty("error")]
        public ProcessingError OccurredError { get; set; }

        /// <summary>
        /// Not null if processing result is ready and it is an object
        /// </summary>
        [JsonConverter(typeof(CustomDataConverter))]
        [JsonProperty("resObj")]
        public string ResultObjectJson { get; set; }

        /// <summary>
        /// Not null if processing result is ready and it is a binary array
        /// </summary>
        [JsonProperty("resBin")]
        public byte[] ResultBin { get; set; }
    }
}
