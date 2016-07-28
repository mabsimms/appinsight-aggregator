using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzureCAT.Samples.AppInsight
{
    public class DebugProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public DebugProcessor(ITelemetryProcessor next)
        {
            this._next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is MetricTelemetry)
            {
                var me = item as MetricTelemetry;
                Console.WriteLine($"{me.Name} {me.Timestamp} average {me.Value} min {me.Min} max {me.Max} count {me.Count}");
                foreach (var p in me.Properties)
                    Console.WriteLine($"\tk {p.Key} = {p.Value}");
            }
            else
            {
                 Console.WriteLine(item.ToString());
            }
           
            this._next.Process(item);
        }
    }
}