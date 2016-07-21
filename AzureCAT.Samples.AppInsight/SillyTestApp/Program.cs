using AzureCAT.Samples.AppInsight.Aggregator;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SillyTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var aggConfig = new TelemetryAggregatorConfig()
            {
                MaxBacklog = 1000,
                MaxWindowEventCount = 10000,
                Window = TimeSpan.FromSeconds(5),
                Target = PublishTarget.Self
            };

            TelemetryConfiguration.Active.InstrumentationKey = "f023a225-b026-45ab-83a9-a3b26e42e497";

            var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
            builder.Use((next) => new TelemetryAggregator(next, aggConfig));
            builder.Use((next) => new DebugProcessor(next));
            builder.Build();

            var tc = new TelemetryClient(TelemetryConfiguration.Active);
            var rand = new Random();

            while (true)
            {
                tc.TrackMetric("sample metric", rand.Next(1000));
                Thread.Sleep(1);
            }
        }
    }
}
