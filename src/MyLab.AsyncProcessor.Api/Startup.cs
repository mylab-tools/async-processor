using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyLab.AsyncProcessor.Api.Services;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.HttpMetrics;
using MyLab.Mq.PubSub;
using MyLab.Redis;
using MyLab.StatusProvider;
using MyLab.Syslog;
using MyLab.WebErrors;
using Prometheus;

namespace MyLab.AsyncProcessor.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(o=> o.AddExceptionProcessing())
                .AddNewtonsoftJson();

            services.AddLogging(c => c.AddSyslog());

            services.AddUrlBasedHttpMetrics();
            services.AddAppStatusProviding();
            services.AddRedisService(Configuration);
            services.AddMqPublisher();
            
            services.Configure<SyslogLoggerOptions>(Configuration.GetSection("Logging:Syslog"));
            services.ConfigureMq(Configuration);

            services.AddMqConsuming(registrar =>
            {
                registrar.RegisterConsumerByOptions<AsyncProcessorOptions, string>(
                    opt => opt.DeadLetter,
                    queue => new MqConsumer<QueueRequestMessage, DeadLetterConsumer>(queue));
            });

            services.Configure<AsyncProcessorOptions>(Configuration.GetSection("AsyncProc"));

            services.Configure<ExceptionProcessingOptions>(o => o.HideError = false);

            services.AddSingleton<Logic>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseHttpMetrics();           
            app.UseUrlBasedHttpMetrics();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });

            app.UseStatusApi();
        }
    }
}
