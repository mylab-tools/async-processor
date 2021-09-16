using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MyLab.AsyncProcessor.Api.Services;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.HttpMetrics;
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

            services.AddRedis(RedisConnectionStrategy.Background);

            services
                .AddRabbit()
                .AddRabbitConsumer<AsyncProcessorOptions, DeadLetterConsumer>(opt => opt.DeadLetter);
            
            services.Configure<SyslogLoggerOptions>(Configuration.GetSection("Logging:Syslog"));

            services.Configure<AsyncProcessorOptions>(Configuration.GetSection("AsyncProc"));

            services.Configure<ExceptionProcessingOptions>(o => o.HideError = false);

            services.ConfigureRedis(Configuration);
            services.ConfigureRabbit(Configuration, "Mq");

            services.AddSingleton<Logic>();

            services.AddHealthChecks()
                .AddRabbit()
                .AddRedis();
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
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResultStatusCodes =
                    {
                        [HealthStatus.Degraded] = StatusCodes.Status200OK,
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Unhealthy] = StatusCodes.Status200OK,
                    }
                });
            });

            app.UseStatusApi();
        }
    }
}
