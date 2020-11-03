using System;

namespace MyLab.AsyncProcessor.Api
{
    public class RequestNotFoundException : Exception
    {
        public RequestNotFoundException(string id): base($"Request '{id}' not found")
        {
            
        }
    }
}
