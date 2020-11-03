using System;
using System.Net.Http;
using System.Text;

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

        public HttpContent ToHttpContent()
        {
            switch (_mimeType)
            {
                case "application/octet-stream":
                {
                    var byteContent = Convert.FromBase64String(_value);
                    return new ByteArrayContent(byteContent);
                }
                case "text/plain":
                {
                    return new StringContent(_value);
                }
                case "application/json":
                {
                    return new StringContent(_value, Encoding.UTF8, "application/json");
                }
                default:
                {
                    throw new UnsupportedMediaTypeException(_mimeType);
                }
            }
        }
    }
}
