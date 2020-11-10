using System;
using Microsoft.AspNetCore.Mvc;

namespace MyLab.AsyncProcessor.Api.Tools
{
    public class RequestResult
    {
        private readonly string _mimeType;
        private readonly string _value;

        public RequestResult(string mimeType, string value)
        {
            _mimeType = mimeType;
            _value = value;
        }

        public IActionResult ToActionResult()
        {
            switch (_mimeType)
            {
                case "application/octet-stream":
                {
                    var byteContent = Convert.FromBase64String(_value);
                    return new FileContentResult(byteContent, _mimeType);
                }
                case "application/json":
                {
                    return new ContentResult(){Content = _value, ContentType = _mimeType};
                }
                default:
                {
                    throw new UnsupportedMediaTypeException(_mimeType);
                }
            }
        }
    }
}
