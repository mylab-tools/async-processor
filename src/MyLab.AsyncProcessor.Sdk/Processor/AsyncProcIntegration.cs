using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Mq;
using MyLab.Mq.PubSub;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    /// <summary>
    /// Extension for integration to app
    /// </summary>
    public static class AsyncProcIntegration
    {
        /// <summary>
        /// Default configuration section name
        /// </summary>
        public const string DefaultConfigSection = "AsyncProc";

        /// <summary>
        /// Adds AsyncProcessor logic
        /// </summary>
        /// <typeparam name="TRequest">request type</typeparam>
        /// <typeparam name="TLogic">processing logic</typeparam>
        /// <param name="services">application services</param>
        /// <param name="config">application configuration</param>
        /// <param name="configSectionName">configuration section name. <see cref="DefaultConfigSection"/> by default</param>
        public static IServiceCollection AddAsyncProcessing<TRequest, TLogic>(this IServiceCollection services, IConfiguration config, string configSectionName = DefaultConfigSection)
            where TRequest : class
            where TLogic : class, IAsyncProcessingLogic<TRequest>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var configSection = config.GetSection(configSectionName);
            services.Configure<AsyncProcessorOptions>(configSection);

            services.ConfigureMq(config);
            services.AddMqConsuming(registrar =>
                registrar.RegisterConsumerByOptions<MyLab.AsyncProcessor.Sdk.Processor.AsyncProcessorOptions>(
                    opt =>
                    {
                        if(opt.Queue == null)
                            throw new InvalidOperationException("Queue not specified in config");
                        return new MqConsumer<QueueRequestMessage, AsyncProcMqConsumingLogic<TRequest>>(opt.Queue);
                    })
            );
            services.AddSingleton<IAsyncProcessingLogic<TRequest>, TLogic>();
            services.AddSingleton<ILostRequestEventHandler, LostRequestEventHandler>();

            return services;
        }
    }
}
