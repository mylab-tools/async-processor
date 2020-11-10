using MyLab.AsyncProcessor.Sdk.Processor;
using Xunit.Abstractions;

namespace IntegrationTests
{
    class WebCallReporterFactory : IWebCallReporterFactory
    {
        private readonly ITestOutputHelper _output;

        public WebCallReporterFactory(ITestOutputHelper output)
        {
            _output = output;
        }

        public IWebCallReporter Create<TContract>()
        {
            return new WebCallReporter(_output, typeof(TContract));
        }
    }
}