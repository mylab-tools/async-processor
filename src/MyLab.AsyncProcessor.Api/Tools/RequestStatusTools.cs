using System;
using System.Linq;
using System.Threading.Tasks;
using MyLab.AsyncProcessor.Sdk;
using MyLab.Redis.ObjectModel;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MyLab.AsyncProcessor.Api.Tools
{
    static class RequestStatusTools
    {
        public static async Task WriteToRedis(this RequestStatus status, HashRedisKey hash)
        {
            var props = new []
            {
                new HashEntry("processStep", status.Step.ToString()),
                new HashEntry("bizStep", status.BizStep),
                new HashEntry("successful", status.Successful),
                new HashEntry("error", JsonConvert.SerializeObject(status.Error)),
                new HashEntry("resultSize", status.ResponseSize),
                new HashEntry("resultMime", status.ResponseMimeType)
            };

            await hash.SetAsync(props);
        }

        public static async Task<RequestStatus> ReadFromRedis(HashRedisKey hash)
        {
            var props = await hash.GetAllAsync();

            return new RequestStatus
            {
                Step = Enum.Parse<ProcessStep>(
                    props.FirstOrDefault(p => p.Name == "processStep")
                        .Value
                        .ToString()
                    ),

                BizStep = props
                    .FirstOrDefault(p => p.Name == "bizStep")
                    .Value
                    .ToString(),

                Successful = bool.Parse(
                    props.FirstOrDefault(p => p.Name == "successful")
                        .Value
                        .ToString()
                    ),

                Error = JsonConvert.DeserializeObject<ProcessingError>(
                    props.FirstOrDefault(p => p.Name == "error")
                        .Value
                        .ToString()
                    ),

                ResponseSize = long.Parse(
                    props.FirstOrDefault(p => p.Name == "resultSize")
                        .Value
                        .ToString()
                    ),

                ResponseMimeType = props
                    .FirstOrDefault(p => p.Name == "resultMime")
                    .Value
                    .ToString(),
            };
        }

        public static async Task<string> ReadResultMimeType(HashRedisKey hash)
        {
            return await hash.GetAsync("responseMime");
        }

        public static async Task SaveResultInfo(long length, string mimeType, HashRedisKey hash)
        {
            var props = new[]
            {
                new HashEntry("resultSize", length),
                new HashEntry("resultMime", mimeType)
            };

            await hash.SetAsync(props);
        }

        public static async Task SaveBizStep(string bizStep, HashRedisKey hash)
        {
            var props = new[]
            {
                new HashEntry("bizStep", bizStep)
            };

            await hash.SetAsync(props);
        }
    }
}
