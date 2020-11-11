using System.Threading.Tasks;
using MyLab.ApiClient;
using MyLab.AsyncProcessor.Sdk.DataModel;

namespace MyLab.AsyncProcessor.Sdk
{
    /// <summary>
    /// Async processor API contract
    /// </summary>
    [Api("v1/requests", Key = ContractKey)]
    public interface IAsyncProcessorRequestsApi
    {
        /// <summary>
        /// Contract key
        /// </summary>
        public const string ContractKey = "async-proc";

        /// <summary>
        /// Creates new request
        /// </summary>
        /// <param name="request">request data</param>
        /// <returns>new request identifier</returns>
        [Post]
        Task<string> CreateAsync([JsonContent] CreateRequest request);

        /// <summary>
        /// Gets request status
        /// </summary>
        /// <param name="id">request id</param>
        [Get("{id}/status")]
        Task<RequestStatus> GetStatusAsync([Path]string id);

        /// <summary>
        /// Update business level step
        /// </summary>
        /// <param name="id">request id</param>
        /// <param name="bizStep">step identifier</param>
        [Put("{id}/status/biz-step")]
        Task UpdateBizStepAsync([Path]string id, [JsonContent]string bizStep);

        /// <summary>
        /// Sets error for request status and complete request
        /// </summary>
        /// <param name="id">request id</param>
        /// <param name="error">error description</param>
        [Put("{id}/status/error")]
        Task CompleteWithErrorAsync([Path]string id, [JsonContent]ProcessingError error);

        /// <summary>
        ///  Sets 'Processing' as value of processing step 
        /// </summary>
        /// <param name="id">request id</param>
        [Post("{id}/status/step/processing")]
        Task MakeRequestProcessing([Path] string id);

        /// <summary>
        ///  Sets 'Completed' as value of processing step 
        /// </summary>
        /// <param name="id">request id</param>
        [Post("{id}/status/step/completed")]
        Task MakeRequestCompleted([Path] string id);

        /// <summary>
        /// Sets object as result and complete request
        /// </summary>
        /// <param name="id">request id</param>
        /// <param name="content">object-content</param>
        [Put("{id}/result")]
        Task CompleteWithObjectResultAsync([Path] string id, [JsonContent] object content);

        /// <summary>
        /// Sets bytes as result and complete request
        /// </summary>
        /// <param name="id">request id</param>
        /// <param name="content">bytes</param>
        [Put("{id}/result")]
        Task CompleteWithBinaryResultAsync([Path] string id, [BinContent] byte[] content);

        /// <summary>
        /// Gets request result as object
        /// </summary>
        /// <typeparam name="T">object type</typeparam>
        /// <param name="id">request identifier</param>
        [Get("{id}/result")]
        Task<T> GetObjectResult<T>([Path] string id);

        /// <summary>
        /// Gets request result as binary
        /// </summary>
        /// <param name="id">request identifier</param>
        [Get("{id}/result")]
        Task<byte[]> GetBinResult([Path] string id);
    }
}
