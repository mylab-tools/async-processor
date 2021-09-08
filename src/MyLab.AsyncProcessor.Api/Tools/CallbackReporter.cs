using System;
using System.Text;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Publishing;

namespace MyLab.AsyncProcessor.Api.Tools
{
    class CallbackReporter
    {
        private readonly IRabbitPublisher _mqPublisher;
        private readonly string _callbackQueue;

        public IDslLogger Log { get; set; }

        public CallbackReporter(IRabbitPublisher mqPublisher, string callbackQueue)
        {
            _mqPublisher = mqPublisher;
            _callbackQueue = callbackQueue;
        }

        public void SendCompletedWithResult(string requestId, string callbackRouting, byte[] resultBin, string mimeType)
        {
            var msg = new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewProcessStep = ProcessStep.Completed
            };

            switch (mimeType)
            {
                case "application/octet-stream":
                {
                    msg.ResultBin = resultBin;
                }
                    break;
                case "application/json":
                {
                    msg.ResultObjectJson = Encoding.UTF8.GetString(resultBin);
                }
                    break;
                default:
                {
                    throw new UnsupportedMediaTypeException(mimeType);
                }
            }


            SendCallbackMessage(
                callbackRouting,
                msg);
        }

        public void SendCompletedWithError(string requestId, string callbackRouting, ProcessingError procError)
        {
            SendCallbackMessage(
                callbackRouting,
                new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewProcessStep = ProcessStep.Completed,
                OccurredError = procError
            });
        }

        public void SendRequestStep(string requestId, string callbackRouting, ProcessStep step)
        {
            SendCallbackMessage(
                callbackRouting,
                new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewProcessStep = step
            });
        }

        public void SendBizStepChanged(string requestId, string callbackRouting, string newBizStep)
        {
            SendCallbackMessage(
                callbackRouting,
                new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewBizStep = newBizStep
            });
        }

        void SendCallbackMessage(string callbackRouting, ChangeStatusCallbackMessage msg)
        {
            try
            {
                _mqPublisher.IntoExchange(_callbackQueue, callbackRouting)
                    .SendJson(msg)
                    .Publish();

            }
            catch (Exception e)
            {
                Log.Error("Can't send callback message", e).Write();
            }
        }
    }
}
