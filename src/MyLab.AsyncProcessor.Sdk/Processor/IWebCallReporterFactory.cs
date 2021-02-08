using MyLab.ApiClient;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Reports about web calls
    /// </summary>
    public interface IWebCallReporter
    {
        /// <summary>
        /// Reports about web call
        /// </summary>
        void Report(CallDetails call);
    }

    /// <summary>
    /// Creates web call reporters
    /// </summary>
    public interface IWebCallReporterFactory
    {
        /// <summary>
        /// Create web call reporters
        /// </summary>
        IWebCallReporter Create<TContract>();
    }
}
