using System;
using MyLab.ApiClient;
using MyLab.ApiClient.Test;
using MyLab.AsyncProcessor.Sdk.Processor;
using Xunit.Abstractions;

namespace IntegrationTests
{
    class WebCallReporter : IWebCallReporter
    {
        private readonly ITestOutputHelper _output;
        private readonly Type _contractType;

        public WebCallReporter(ITestOutputHelper output, Type contractType)
        {
            _output = output;
            _contractType = contractType;
        }
        public void Report(CallDetails call)
        {
            _output.WriteLine(call.ToTestDump(_contractType));
        }
    }
}