using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Mq;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Extension for integration to app
    /// </summary>
    public static class AsyncProcIntegration
    {
        /// <summary>
        /// Adds AsyncProcessor logic
        /// </summary>
        /// <typeparam name="TRequest">request type</typeparam>
        /// <typeparam name="TLogic">processing logic</typeparam>
        /// <param name="services">application services</param>
        /// <param name="config">application configuration</param>
        public static IServiceCollection AddAsyncProcessing<TRequest, TLogic>(this IServiceCollection services, IConfiguration config)
            where TRequest : class
            where TLogic : class, IAsyncProcessingLogic<TRequest>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var configSection = config.GetSection("AsyncProc");
            services.Configure<AsyncProcessorOptions>(configSection);

            var opt = configSection.Get<AsyncProcessorOptions>();

            services.LoadMqConfig(config);
            services.AddMqConsuming(registrar =>
                registrar.RegisterConsumer(
                    new MqConsumer<QueueRequestMessage, AsyncProcMqConsumingLogic<TRequest>>(opt.Queue)
                )
            );
            services.AddSingleton<IAsyncProcessingLogic<TRequest>, TLogic>();

            return services;
        }
    }
}
