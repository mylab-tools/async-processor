using System.Threading.Tasks;
using MyLab.ApiClient;
using MyLab.AsyncProcessor.Sdk.DataModel;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    class ProcessingOperator : IProcessingOperator
    {
        private readonly string _requestId;
        private readonly ApiClient<IAsyncProcessorRequestsApi> _api;

        public IWebCallReporter Reporter { get; set; }

        public ProcessingOperator(string requestId, ApiClient<IAsyncProcessorRequestsApi> api)
        {
            _requestId = requestId;
            _api = api;
        }

        public async Task SetBizStepAsync(string bizStep)
        {
            var details = await _api.Request(s => s.UpdateBizStepAsync(_requestId, bizStep)).GetDetailedAsync();

            Reporter?.Report(details);
        }

        public async Task CompleteWithErrorAsync(ProcessingError error)
        {
            var details = await _api.Request(s => s.CompleteWithErrorAsync(_requestId, error)).GetDetailedAsync();

            Reporter?.Report(details);
        }

        public async Task CompleteWithResultAsync(object objectResult)
        {
            var details = await _api.Request(s => s.CompleteWithObjectResultAsync(_requestId, objectResult)).GetDetailedAsync();

            Reporter?.Report(details);
        }

        public async  Task CompleteWithResultAsync(byte[] binaryResult)
        {
            var details = await _api.Request(s => s.CompleteWithBinaryResultAsync(_requestId, binaryResult)).GetDetailedAsync();

            Reporter?.Report(details);
        }

        public async  Task CompleteAsync()
        {
            var details = await _api.Request(s => s.MakeRequestCompleted(_requestId)).GetDetailedAsync();

            Reporter?.Report(details);
        }
    }
}