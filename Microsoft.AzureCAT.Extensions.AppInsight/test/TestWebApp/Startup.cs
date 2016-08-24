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
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AzureCAT.Extensions.AppInsight;
using Microsoft.AzureCAT.Extensions.AppInsight.Sinks;
using Microsoft.WindowsAzure.Storage;
using Swashbuckle.Swagger.Model;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Microsoft.AzureCAT.Extensions.AppInsight.Utils;

namespace TestWebApp
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
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
            
            
            var aiClientBuilder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;

            // Publish raw events directly to blob storage
            var blobConfigSection = Configuration.GetSection("ApplicationInsights")
                .GetSection("BlobPublisher");
            var storageAccountString = blobConfigSection.GetValue<string>("BlobAccount");
            var containerName = blobConfigSection.GetValue<string>("ContainerName");

            var storageAccount = CloudStorageAccount.Parse(storageAccountString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(containerName);

            var createdContainer = blobContainer.CreateIfNotExistsAsync().Result;
            if (createdContainer)
            {
                
            }
                        
            aiClientBuilder.Use((next) => new BlobContainerSink(
                next: next,
                blobContainer: blobContainer,
                // TODO - put the real naming function in here
                blobPathFunc: () => String.Format("todo.json"),
                // TODO - adjust the callback to infer schema name from the type
                // TODO - adjust the blob writer to handle interleaved schema
                onBlobWrittenFunc: async (blob) => await OpenSchemaCallback.PostCallback(                    
                    blob: blob,
                    endpoint: new Uri(""),
                    schemaName: "TODO",
                    iKey: Configuration.GetSection("ApplicationInsights")
                        .GetValue<string>("InstrumentationKey")),
                bufferSize: 4*1024));

            // Set up an in-memory aggregator
            var aggConfigSection = Configuration.GetSection("ApplicationInsights")
                .GetSection("SlidingWindow");
            var windowSpan = aggConfigSection.GetValue<TimeSpan>(
                "WindowSpan", TimeSpan.FromSeconds(5));
            var publishRawEvents = aggConfigSection.GetValue<bool>(
                "PublishRawEvents", true);

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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
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
