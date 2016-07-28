using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureCAT.Samples.AppInsight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting this up!");
            var loggerFactory = new LoggerFactory()
                .AddConsole()
            ;

            var cfgBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", 
                    optional: true, reloadOnChange: true);                        
            var config = cfgBuilder.Build();

            var key = config.GetValue<string>("ApplicationInsights:InstrumentationKey");
            TelemetryConfiguration.Active.InstrumentationKey = key;
        
            var configSection = config
                .GetSection("ApplicationInsights")
                .GetSection("SlidingWindow"); 
            var windowSpan = configSection.GetValue<TimeSpan>(
                "WindowSpan", TimeSpan.FromSeconds(5));
            var publishRawEvents = configSection.GetValue<bool>(
                "PublishRawEvents", false);
            Console.WriteLine($"ws = {windowSpan}, pub = {publishRawEvents}");

            var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
            builder.Use((next) => new TelemetryAggregator(
                next : next, 
                funcs: new DefaultAppInsightPipeline(),
                windowSpan: windowSpan,
                publishRawEvents: publishRawEvents));
            builder.Use((next) => new DebugProcessor(next));
            builder.Build();

            var tc = new TelemetryClient(TelemetryConfiguration.Active);
            var rand = new Random();

            while (true)
            {
                tc.TrackMetric("sample metric", rand.Next(1000));
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}
