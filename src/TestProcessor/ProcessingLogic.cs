using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationTest.Share;
using MyLab.AsyncProcessor.Sdk;
using MyLab.AsyncProcessor.Sdk.Processor;

namespace TestProcessor
{
    class ProcessingLogic : IAsyncProcessingLogic<TestRequest>
    {
        public Task ProcessAsync(TestRequest request, IProcessingOperator op)
        {
            switch (request.Command)
            {
                case "concat":
                    return op.CompleteWithResultAsync(request.Value1 + "-" + request.Value2);
                case "incr-int":
                    return op.CompleteWithResultAsync(request.Value2+1);
                case "str-to-bin":
                    return op.CompleteWithResultAsync(Encoding.UTF8.GetBytes(request.Value1));
                case "unhandled-exception":
                    throw new InvalidOperationException("foo");
                case "reported-error":
                    return op.CompleteWithErrorAsync(new InvalidOperationException("bar").ToProcessingError(bizMsg: "foo"));
                case "biz-step":
                    return op.SetBizStepAsync("foo-step");
                case "interrupt":
                    throw new InterruptConsumingException();
                case "repeat-str":
                    return op.CompleteWithResultAsync(string.Join(',', Enumerable.Repeat(request.Value1, request.Value2)));
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}