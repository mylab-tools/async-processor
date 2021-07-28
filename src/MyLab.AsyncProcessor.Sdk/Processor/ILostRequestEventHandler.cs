using System.Net.Http;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Handles event when request not found
    /// </summary>
    public interface ILostRequestEventHandler
    {
        /// <summary>
        /// Handles
        /// </summary>
        void Handle(string reqId);
    }

    class LostRequestEventHandler : ILostRequestEventHandler
    {
        private readonly IDslLogger _log;

        public LostRequestEventHandler(ILogger<LostRequestEventHandler> logger = null)
        {
            _log = logger?.Dsl();
        }

        public void Handle(string reqId)
        {
            _log?.Warning("Request not found. Processing will bw skipped.")
                .AndFactIs("req-id", reqId)
                .Write();
        }
    }
}