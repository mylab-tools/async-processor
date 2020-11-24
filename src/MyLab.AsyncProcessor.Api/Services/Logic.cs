using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.AsyncProcessor.Api.Tools;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.LogDsl;
using MyLab.Logging;
using MyLab.Mq;
using MyLab.Mq.PubSub;
using MyLab.Redis;
using MyLab.Redis.ObjectModel;

namespace MyLab.AsyncProcessor.Api.Services
{
    public class Logic
    {
        private readonly IMqPublisher _mqPublisher;
        private readonly IRedisService _redis;
        private readonly AsyncProcessorOptions _options;
        private readonly CallbackReporter _callbackReporter;

        public Logic(
            IRedisService redis, 
            IMqPublisher mqPublisher,
            IOptions<AsyncProcessorOptions> options,
            ILogger<Logic> logger = null)
        {
            _mqPublisher = mqPublisher;
            _redis = redis;
            _options = options.Value;

            var log = logger?.Dsl();

            var callBackQueue = _options?.Callback;
            if (!string.IsNullOrEmpty(callBackQueue))
                _callbackReporter = new CallbackReporter(_mqPublisher, callBackQueue)
                {
                    Log = log
                };
        }

        public async Task<string> RegisterNewRequestAsync()
        {
            var newId = Guid.NewGuid().ToString("N");
            
            var statusKey = _redis.Db().Hash(CreateKeyName(newId, "status"));

            var initialStatus = new RequestStatus
            {
                Step = ProcessStep.Pending
            };

            _callbackReporter?.SendStartProcessing(newId);

            await initialStatus.WriteToRedis(statusKey);
            await statusKey.ExpireAsync(_options.MaxIdleTime);

            return newId;
        }

        public void SendRequestToProcessor(string id, CreateRequest createRequest)
        {
            var msgPayload = new QueueRequestMessage
            {
                Id = id,
                Content = createRequest.Content
            };

            var msg =new OutgoingMqEnvelop<QueueRequestMessage>
            {
                Message = new MqMessage<QueueRequestMessage>(msgPayload),
                PublishTarget = new PublishTarget
                {
                    Exchange = _options.QueueExchange,
                    Routing = createRequest.Routing ?? _options.QueueRoutingKey
                }
            };

            _mqPublisher.Publish(msg);
        }

        public async Task<RequestStatus> GetStatusAsync(string id)
        {
            var key = await GetStatusKey(id);

            return await RequestStatusTools.ReadFromRedis(key);
        }

        public async Task SetBizStepAsync(string id, string bizStep)
        {
            var key = await GetStatusKey(id);

            await RequestStatusTools.SaveBizStep(bizStep, key);
            await key.ExpireAsync(_options.MaxIdleTime);

            _callbackReporter?.SendBizStepChanged(id, bizStep);
        }

        public async Task CompleteWithErrorAsync(string id, ProcessingError error)
        {
            var key = await GetStatusKey(id);

            await RequestStatusTools.SaveError(error, key);
            await key.ExpireAsync(_options.MaxStoreTime);

            _callbackReporter?.SendCompletedWithError(id, error);
        }

        public async Task SetRequestStep(string id, ProcessStep processStep)
        {
            var key = await GetStatusKey(id);

            await RequestStatusTools.SetStep(processStep, key);
            await key.ExpireAsync(_options.MaxIdleTime);

            _callbackReporter?.SendRequestStep(id, processStep);
        }

        public async Task<RequestResult> GetResultAsync(string id)
        {
            var statusKey = await GetStatusKey(id);
            var resultMime = await RequestStatusTools.ReadResultMimeType(statusKey);

            if (resultMime == null)
                throw new RequestResultNotReadyException()
                    .AndFactIs("reques-id", id);

            var resultKey = GetResultKey(id);
            var resultContent = await resultKey.GetAsync();

            return new RequestResult(resultMime, resultContent);
        }

        public async Task CompleteWithResultAsync(string id, byte[] content, string mimeType)
        {
            var statusKey = await GetStatusKey(id);

            await RequestStatusTools.SaveResult(content.Length, mimeType, statusKey);

            var resultKey = GetResultKey(id);
            var strContent = ContentToString(content, mimeType);
            await resultKey.SetAsync(strContent);

            await statusKey.ExpireAsync(_options.MaxStoreTime);
            await resultKey.ExpireAsync(_options.MaxStoreTime);

            _callbackReporter?.SendCompletedWithResult(id, content, mimeType);
        }

        string CreateKeyName(string id, string suffix) => _options.RedisKeyPrefix.TrimEnd(':') + ':' + id + ":" + suffix;

        string ContentToString(byte[] content, string mimeType)
        {
            switch (mimeType)
            {
                case "application/octet-stream":
                {
                    return Convert.ToBase64String(content);
                }
                case "application/json":
                {
                    return Encoding.UTF8.GetString(content);
                }
                default:
                {
                    throw new UnsupportedMediaTypeException(mimeType);
                }
            }
        }

        async Task<HashRedisKey> GetStatusKey(string id)
        {
            var statusKeyName = CreateKeyName(id, "status");
            var statusKey = _redis.Db().Hash(statusKeyName);

            if (!await statusKey.ExistsAsync())
                throw new RequestNotFoundException()
                    .AndFactIs("reques-id", id);

            return statusKey;
        }

        StringRedisKey GetResultKey(string id)
        {
            var resultKeyName = CreateKeyName(id, "result");
            return _redis.Db().String(resultKeyName);
        }
    }
}
