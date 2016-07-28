using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using MathNet.Numerics.Statistics;

namespace AzureCAT.Samples.AppInsight
{
    public class DefaultAppInsightPipeline : IPipelineFunctions
    {
        public bool Filter(ITelemetry evt)
        {
            return !(evt is MetricTelemetry);
        }

        public string GetName(ITelemetry evt)
        {
            if (evt is MetricTelemetry)
                return (evt as MetricTelemetry).Name;
            return "";
        }

        public IEnumerable<ITelemetry> Transform(IEnumerable<ITelemetry> evts)
        {
              return evts
                    .OfType<MetricTelemetry>()
                    .GroupBy(e => new { e.Name })
                    .Select(e => new MetricTelemetryCollection() 
                    {
                        Event = new MetricTelemetry () {
                            Name = e.Key.Name,
                            Value = e.Average(t => t.Value),
                            Timestamp = e.First().Timestamp,
                            Min = e.Min(t => t.Value),
                            Max = e.Max(t => t.Value),
                            Count = e.Count(),
                            StandardDeviation = e.StdDev(t => t.Value),

                        // Use the merge method to pull the percentiles into the
                        // proprties dictionary                     
                        },
                        Properties = new Dictionary<string, string>() { 
                            { "P50", Statistics.Percentile(e.Select(t=> t.Value), 50).ToString() },
                            { "P90", Statistics.Percentile(e.Select(t=> t.Value), 90).ToString() },
                            { "P99", Statistics.Percentile(e.Select(t=> t.Value), 99).ToString() } 
                        }
                    })
                    .Select(e => e.Merge())
                ;
        }
    }

    public class MetricTelemetryCollection 
    {
        public MetricTelemetry Event { get; set; }
        public IDictionary<string, string> Properties { get; set; }

        public MetricTelemetry Merge()
        {
            foreach (var nr in Properties)
                Event.Properties.Add(nr.Key, nr.Value);
            return Event;
        }
    }
}