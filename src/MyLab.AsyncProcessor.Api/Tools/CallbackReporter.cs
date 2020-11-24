using System;
using System.Text;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.LogDsl;
using MyLab.Mq;
using MyLab.Mq.PubSub;

namespace MyLab.AsyncProcessor.Api.Tools
{
    class CallbackReporter
    {
        private readonly IMqPublisher _mqPublisher;
        private readonly string _callbackQueue;

        public DslLogger Log { get; set; }

        public CallbackReporter(IMqPublisher mqPublisher, string callbackQueue)
        {
            _mqPublisher = mqPublisher;
            _callbackQueue = callbackQueue;
        }

        public void SendStartProcessing(string requestId)
        {
            SendCallbackMessage(new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewProcessStep = ProcessStep.Processing
            });
        }

        public void SendCompletedWithResult(string requestId, byte[] resultBin, string mimeType)
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


            SendCallbackMessage(msg);
        }

        public void SendCompletedWithError(string requestId, ProcessingError procError)
        {
            SendCallbackMessage(new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewProcessStep = ProcessStep.Completed,
                OccurredError = procError
            });
        }

        public void SendRequestStep(string requestId, ProcessStep step)
        {
            SendCallbackMessage(new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewProcessStep = step
            });
        }

        public void SendBizStepChanged(string requestId, string newBizStep)
        {
            SendCallbackMessage(new ChangeStatusCallbackMessage
            {
                RequestId = requestId,
                NewBizStep = newBizStep
            });
        }

        void SendCallbackMessage(ChangeStatusCallbackMessage msg)
        {
            try
            {
                _mqPublisher.Publish(new OutgoingMqEnvelop<ChangeStatusCallbackMessage>
                {
                    PublishTarget = new PublishTarget { Routing = _callbackQueue },
                    Message = new MqMessage<ChangeStatusCallbackMessage>(msg)
                });
            }
            catch (Exception e)
            {
                Log.Error("Can't send callback message", e).Write();
            }
        }
    }
}
