using System;

namespace MyLab.AsyncProcessor.Api
{
    public class RequestNotFoundException : Exception
    {
        public RequestNotFoundException(): base($"Request not found")
        {
            
        }
    }
}
