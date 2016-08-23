using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AzureCAT.Extensions.AppInsight;
using Microsoft.AzureCAT.Extensions.AppInsight.Sinks;
using Swashbuckle.Swagger.Model;

namespace TestWebApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();

            var loggerFactory = new LoggerFactory()
               .AddConsole(LogLevel.Debug);
            var logger = loggerFactory.CreateLogger("Core");

            // Use the in-memory pipeline publishing channel
            TelemetryConfiguration.Active.TelemetryChannel =
                new InMemoryPublishingChannel(500, TimeSpan.FromSeconds(30));

            // Set up a custom app insights pipeline
            var configSection = Configuration
              .GetSection("ApplicationInsights")
              .GetSection("SlidingWindow");
            var windowSpan = configSection.GetValue<TimeSpan>(
                "WindowSpan", TimeSpan.FromSeconds(5));
            var publishRawEvents = configSection.GetValue<bool>(
                "PublishRawEvents", true);
            
            var aiClientBuilder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
            aiClientBuilder.Use((next) => new InMemoryAggregator(
                next: next,
                funcs: new DefaultAppInsightAggregator(),
                windowSpan: windowSpan,
                publishRawEvents: publishRawEvents,
                logger: logger));
            // TODO - get graphite target config
            // aiClientBuilder.Use((next) => new GraphitePublisher(next, logger, "localhost"));         
            aiClientBuilder.Build(); 
        }

        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            var pathToDoc = Configuration["Swagger:Path"];

            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc();

            services.AddSwaggerGen();
            services.ConfigureSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "Sample API",
                    Description = "A simple api to demonstrate ai extensions",
                    TermsOfService = "None"
                });
                // TODO
                //options.IncludeXmlComments(pathToDoc);
                options.DescribeAllEnumsAsStrings();
            });          
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseApplicationInsightsRequestTelemetry();
            app.UseApplicationInsightsExceptionTelemetry();

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUi();
        }
    }
}
