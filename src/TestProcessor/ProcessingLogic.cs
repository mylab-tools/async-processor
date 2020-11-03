using System.Threading.Tasks;
using IntegrationTest.Share;
using MyLab.AsyncProcessor.Sdk.Processor;

namespace TestProcessor
{
    class ProcessingLogic : IAsyncProcessingLogic<TestRequest>
    {
        public Task ProcessAsync(TestRequest request, IProcessingOperator op)
        {
            var newVal = request.Value1 + "-" + request.Value2;
            return op.CompleteProcessingWithResultAsync(newVal);
        }
    }
}