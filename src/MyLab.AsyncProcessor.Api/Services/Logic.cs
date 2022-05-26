using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.AsyncProcessor.Api.Models;
using MyLab.AsyncProcessor.Api.Tools;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Log;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Publishing;
using MyLab.Redis;
using MyLab.Redis.ObjectModel;
using MyLab.Redis.Services;

namespace MyLab.AsyncProcessor.Api.Services
{
    public class Logic
    {
        private readonly IRabbitPublisher _mqPublisher;
        private readonly IRedisService _redis;
        private readonly AsyncProcessorOptions _options;
        private readonly CallbackReporter _callbackReporter;

        public Logic(
            IRedisService redis,
            IRabbitPublisher mqPublisher,
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

        public async Task<string> RegisterNewRequestAsync(string preassignedId = null)
        {
            var resId = preassignedId ?? Guid.NewGuid().ToString("N");
            
            var statusKey = _redis.Db().Hash(CreateKeyName(resId, "status"));

            var initialStatus = new RequestStatus
            {
                Step = ProcessStep.Pending
            };

            await initialStatus.WriteToRedis(statusKey);
            await statusKey.ExpireAsync(_options.ProcessingTimeout);

            return resId;
        }

        public async Task SendRequestToProcessorAsync(string id, CreateRequest createRequest, HttpRequest httpRequest)
        {
            var msgPayload = new QueueRequestMessage
            {
                Id = id,
                Content = createRequest.Content,
                IncomingDt = DateTime.Now
            };

            if (httpRequest.Headers != null)
            {
                msgPayload.Headers = httpRequest.Headers                
                    .ToDictionary(                    
                            h => h.Key,                     
                            h=> h.Value.ToString()
                        );
            }

            _mqPublisher.IntoExchange(
                    _options.QueueExchange, 
                    createRequest.ProcRouting ?? _options.QueueRoutingKey)
                .SendJson(msgPayload)
                .AndProperty(p => p.Expiration = ((long)_options.ProcessingTimeout.TotalMilliseconds).ToString())
                .Publish();

            if(createRequest.CallbackRouting != null)
            {
                var callBackRoutingKey = GetCallbackRoutingKey(id);
                await callBackRoutingKey.SetAsync(createRequest.CallbackRouting);
                await callBackRoutingKey.ExpireAsync(_options.ProcessingTimeout);
            }
        }

        public async Task<RequestStatus> GetStatusAsync(string id)
        {
            var key = await GetStatusKeyAsync(id);

            return await RequestStatusTools.ReadFromRedis(key);
        }

        public async Task SetBizStepAsync(string id, string bizStep)
        {
            var key = await GetStatusKeyAsync(id);

            await RequestStatusTools.SaveBizStep(bizStep, key);

            var callbackRouting = await GetCallbackRoutingAsync(id);
            _callbackReporter?.SendBizStepChanged(id, callbackRouting, bizStep);
        }

        public async Task CompleteWithErrorAsync(string id, ProcessingError error)
        {
            var key = await GetStatusKeyAsync(id);

            await RequestStatusTools.SaveError(error, key);
            await UpdateStoreExpiration(id);

            var callbackRouting = await GetCallbackRoutingAsync(id);
            _callbackReporter?.SendCompletedWithError(id, callbackRouting, error);
        }

        public async Task SetRequestStepAsync(string id, ProcessStep processStep)
        {
            var key = await GetStatusKeyAsync(id);

            await RequestStatusTools.SetStep(processStep, key);

            var callbackRouting = await GetCallbackRoutingAsync(id);
            _callbackReporter?.SendRequestStep(id, callbackRouting, processStep);
        }

        public async Task<RequestResult> GetResultAsync(string id)
        {
            var statusKey = await GetStatusKeyAsync(id);
            var resultMime = await RequestStatusTools.ReadResultMimeType(statusKey);

            if (resultMime == null)
                throw new RequestResultNotReadyException()
                    .AndFactIs("reques-id", id);

            var resultKey = GetResultKey(id);
            var resultContent = await resultKey.GetAsync();

            return new RequestResult(resultMime, resultContent);
        }

        public async Task<RequestDetails> GetRequestDetailsAsync(string id, bool includeResponse)
        {
            var statusKey = await GetStatusKeyAsync(id);
            var status = await RequestStatusTools.ReadFromRedis(statusKey);

            if (status == null)
                throw new RequestResultNotReadyException()
                    .AndFactIs("reques-id", id);

            var reqDetails = new RequestDetails
            {
                Id = id,
                Status = status
            };

            if (status.Successful && includeResponse)
            {
                var resultKey = GetResultKey(id);
                var resultContent = await resultKey.GetAsync();

                reqDetails.SetResponse(resultContent);
            }

            return reqDetails;
        }

        public async Task CompleteWithResultAsync(string id, byte[] content, string mimeType)
        {
            var statusKey = await GetStatusKeyAsync(id);

            await RequestStatusTools.SaveResult(content.Length, mimeType, statusKey);

            var resultKey = GetResultKey(id);
            var strContent = ContentToString(content, mimeType);
            await resultKey.SetAsync(strContent);

            await UpdateStoreExpiration(id);

            var callbackRouting = await GetCallbackRoutingAsync(id);
            _callbackReporter?.SendCompletedWithResult(id, callbackRouting, content, mimeType);
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

        async Task<HashRedisKey> GetStatusKeyAsync(string id)
        {
            var statusKeyName = CreateKeyName(id, "status");
            var statusKey = _redis.Db().Hash(statusKeyName);

            if (!await statusKey.ExistsAsync())
                throw new RequestNotFoundException()
                    .AndFactIs("request-id", id);

            return statusKey;
        }

        StringRedisKey GetResultKey(string id)
        {
            var resultKeyName = CreateKeyName(id, "result");
            var resultKey = _redis.Db().String(resultKeyName);

            return resultKey;
        }

        StringRedisKey GetCallbackRoutingKey(string id)
        {
            var callbackKeyName = CreateKeyName(id, "callback-routing");
            var callbackKey = _redis.Db().String(callbackKeyName);

            return callbackKey;
        }

        async Task<string> GetCallbackRoutingAsync(string id)
        {
            var key = GetCallbackRoutingKey(id);
            var val = await key.GetAsync();
            return val.HasValue ? (string)val : null;
        }

        async Task UpdateStoreExpiration(string id)
        {
            var statusKey = await GetStatusKeyAsync(id);
            var callbackKey = GetCallbackRoutingKey(id);
            var resultKey = GetResultKey(id);

            await statusKey.ExpireAsync(_options.RestTimeout);
            await callbackKey.ExpireAsync(_options.RestTimeout);
            await resultKey.ExpireAsync(_options.RestTimeout);
        }
    }
}
