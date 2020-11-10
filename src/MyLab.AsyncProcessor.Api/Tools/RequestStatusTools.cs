using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Logging;
using MyLab.Redis.ObjectModel;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MyLab.AsyncProcessor.Api.Tools
{
    static class RequestStatusTools
    {
        public static async Task WriteToRedis(this RequestStatus status, HashRedisKey hash)
        {
            var setProps = new List<HashEntry>();
            var delProps = new List<string>();

            ProcProperty("processStep", status.Step.ToString());
            ProcProperty("bizStep", status.BizStep);
            ProcProperty("successful", status.Successful.ToString());
            ProcProperty("resultSize", status.ResponseSize);
            ProcProperty("resultMime", status.ResponseMimeType);

            if(status.Error!= null)
                setProps.Add(new HashEntry("error", JsonConvert.SerializeObject(status.Error)));
            else
            {
                delProps.Add("error");
            }

            await hash.DeleteFieldsAsync(delProps.ToArray());
            await hash.SetAsync(setProps.ToArray());

            void ProcProperty(string name, RedisValue propVal)
            {
                if (!propVal.IsNull)
                {
                    setProps.Add(new HashEntry(name, propVal));
                }
                else
                    delProps.Add(name);
            }
        }

        public static async Task<RequestStatus> ReadFromRedis(HashRedisKey hash)
        {
            var props = await hash.GetAllAsync();

            var status = new RequestStatus();

            foreach (var prop in props)
            {
                switch (prop.Name.ToString())
                {
                    case "processStep":
                        status.Step = Enum.Parse<ProcessStep>(prop.Value);
                        break;
                    case "bizStep":
                        status.BizStep = prop.Value;
                        break;
                    case "successful":
                        status.Successful = bool.Parse(prop.Value);
                        break;
                    case "error":
                        status.Error = JsonConvert.DeserializeObject<ProcessingError>(prop.Value);
                        break;
                    case "resultSize":
                        status.ResponseSize = long.Parse(prop.Value);
                        break;
                    case "resultMime":
                        status.ResponseMimeType = prop.Value;
                        break;
                    default: throw new IndexOutOfRangeException($"Invalid property '{prop}'");
                }
            }

            return status;
        }

        public static async Task<string> ReadResultMimeType(HashRedisKey hash)
        {
            return await hash.GetAsync("responseMime");
        }

        public static async Task SaveResult(long length, string mimeType, HashRedisKey hash)
        {
            var props = new[]
            {
                new HashEntry("processStep", ProcessStep.Completed.ToString()),
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

        public static async Task SaveError(ProcessingError error, HashRedisKey hash)
        {
            var props = new[]
            {
                new HashEntry("processStep", ProcessStep.Completed.ToString()),
                new HashEntry("error", JsonConvert.SerializeObject(error))
            };

            await hash.SetAsync(props);
        }

        public static async Task SetStep(ProcessStep step, HashRedisKey hash)
        {
            var props = new[]
            {
                new HashEntry("processStep", step.ToString())
            };

            await hash.SetAsync(props);
        }
    }
}
