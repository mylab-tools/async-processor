using System;
using MyLab.AsyncProcessor.Sdk.DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyLab.AsyncProcessor.Api.Models
{
    static class RequestDetailsExtensions
    {
        public static void SetResponse(this RequestDetails reqDetails, string response)
        {
            switch (reqDetails.Status.ResponseMimeType)
            {
                case "application/octet-stream":
                {
                    var byteContent = Convert.FromBase64String(response);
                    reqDetails.ResultBin = byteContent;
                    break;
                }
                case "application/json":
                {
                    reqDetails.ResultObject = JToken.Parse(response);
                    break;
                }
                default:
                {
                    throw new UnsupportedMediaTypeException(reqDetails.Status.ResponseMimeType);
                }
            }
        }
    }
}