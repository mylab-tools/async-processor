using System;
using System.Threading.Tasks;
using MyLab.AsyncProcessor.Sdk.DataModel;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    class ProcessingOperator : IProcessingOperator
    {
        private readonly string _requestId;
        private readonly IAsyncProcessorRequestsApi _api;

        public ProcessingOperator(string requestId, IAsyncProcessorRequestsApi api)
        {
            _requestId = requestId;
            _api = api;
        }

        public Task SetBizStepAsync(string bizStep)
        {
            return _api.UpdateBizStepAsync(_requestId, bizStep);
        }

        public Task CompleteWithErrorAsync(string techMessage, string userFriendlyMessage = null)
        {
            return _api.CompleteWithErrorAsync(_requestId, new ProcessingError
            {
                TechMessage = techMessage,
                BizMessage = userFriendlyMessage
            });
        }

        public Task CompleteWithErrorAsync(string userFriendlyMessage, Exception e)
        {
            return _api.CompleteWithErrorAsync(_requestId, new ProcessingError
            {
                TechMessage = e.Message,
                TechInfo = e.ToString(),
                BizMessage = userFriendlyMessage
            });
        }

        public Task CompleteWithErrorAsync(Exception e)
        {
            return _api.CompleteWithErrorAsync(_requestId, new ProcessingError
            {
                TechMessage = e.Message,
                TechInfo = e.ToString()
            });
        }

        public Task CompleteWithResultAsync(object objectResult)
        {
            return _api.CompleteWithObjectResultAsync(_requestId, objectResult);
        }

        public Task CompleteWithResultAsync(byte[] binaryResult)
        {
            return _api.CompleteWithBinaryResultAsync(_requestId, binaryResult);
        }

        public Task CompleteAsync()
        {
            return _api.MakeRequestCompleted(_requestId);
        }
    }
}