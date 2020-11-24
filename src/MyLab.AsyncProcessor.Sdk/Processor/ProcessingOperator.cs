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
            var details = await _api.Call(s => s.UpdateBizStepAsync(_requestId, bizStep)).GetDetailed();

            Reporter?.Report(details);
        }

        public async Task CompleteWithErrorAsync(ProcessingError error)
        {
            var details = await _api.Call(s => s.CompleteWithErrorAsync(_requestId, error)).GetDetailed();

            Reporter?.Report(details);
        }

        public async Task CompleteWithResultAsync(object objectResult)
        {
            var details = await _api.Call(s => s.CompleteWithObjectResultAsync(_requestId, objectResult)).GetDetailed();

            Reporter?.Report(details);
        }

        public async  Task CompleteWithResultAsync(byte[] binaryResult)
        {
            var details = await _api.Call(s => s.CompleteWithBinaryResultAsync(_requestId, binaryResult)).GetDetailed();

            Reporter?.Report(details);
        }

        public async  Task CompleteAsync()
        {
            var details = await _api.Call(s => s.MakeRequestCompleted(_requestId)).GetDetailed();

            Reporter?.Report(details);
        }
    }
}