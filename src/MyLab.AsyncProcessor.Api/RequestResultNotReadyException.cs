using System;

namespace MyLab.AsyncProcessor.Api
{
    public class RequestResultNotReadyException : Exception
    {
        public RequestResultNotReadyException() :base("Request result is not ready")
        {
            
        }
    }
}
