using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MyLab.AsyncProcessor.Api.Services;
using MyLab.HttpMetrics;
using MyLab.Log;
using MyLab.RabbitClient;
using MyLab.Redis;
using MyLab.StatusProvider;
using MyLab.WebErrors;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
                .AddNewtonsoftJson(o => o.SerializerSettings.NullValueHandling = NullValueHandling.Ignore);

            services.AddLogging(c => c.AddMyLabConsole())
                .AddUrlBasedHttpMetrics()
                .AddAppStatusProviding()
                .AddRedis(RedisConnectionStrategy.Background)
                .AddRabbit(RabbitConnectionStrategy.Background)
                .AddRabbitConsumer<AsyncProcessorOptions, DeadLetterConsumer>(opt => opt.DeadLetter, true);

            services
                .Configure<AsyncProcessorOptions>(Configuration.GetSection("AsyncProc"))
                .Configure<ExceptionProcessingOptions>(o => o.HideError = false)
                .ConfigureRedis(Configuration)
                .ConfigureRabbit(Configuration, "Mq");

            services.AddSingleton<Logic>();

            services.AddHealthChecks()
                .AddRabbit()
                .AddRedis();

            var otlpConfig = Configuration.GetSection("Otlp");

            if (otlpConfig.Exists())
            {
                var asyncProcOtlpOptions = new AsyncProcOtlpOptions();

                otlpConfig.Bind(asyncProcOtlpOptions);

                if (asyncProcOtlpOptions.Enabled)
                {
                    services.AddOpenTelemetryTracing(b => b
                            .AddRabbitSource()
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                                .AddEnvironmentVariableDetector()
                                .AddTelemetrySdk()
                                .AddService(asyncProcOtlpOptions.ServiceName))
                            .AddOtlpExporter()
                        )
                        .Configure<OtlpExporterOptions>(otlpConfig)
                        .AddRabbitTracing();
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseRouting()
                .UseHttpMetrics()
                .UseUrlBasedHttpMetrics()
                .UseEndpoints(endpoints =>
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
                })
                .UseStatusApi();
        }
    }
}
