using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Represent request for async processing
    /// </summary>
    /// <typeparam name="T">request content</typeparam>
    public class AsyncProcRequest<T>
    {
        /// <summary>
        /// RequestId
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Request content
        /// </summary>
        public T Content { get; }

        /// <summary>
        /// Initial headers 
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncProcRequest{T}"/>
        /// </summary>
        public AsyncProcRequest(string id, T content, IDictionary<string, string> headers)
        {
            Id = id;
            Content = content;
            Headers = new ReadOnlyDictionary<string, string>(headers);
        }
    }
}