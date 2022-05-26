using System;
using System.Collections.Generic;
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
        /// Incoming date time
        /// </summary>
        [JsonProperty("incomingDt")]
        public DateTime IncomingDt { get; set; }

        /// <summary>
        /// Request content
        /// </summary>
        [JsonProperty("content")]
        [JsonConverter(typeof(CustomDataConverter))]
        public string Content { get; set; }

        /// <summary>
        /// Contains headers from initial http request
        /// </summary>
        [JsonProperty("headers")]
        public Dictionary<string,string> Headers { get; set; }
        
    }
}
