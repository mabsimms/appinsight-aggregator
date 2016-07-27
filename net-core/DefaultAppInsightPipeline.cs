using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureCAT.Samples.AppInsight
{
    public class DefaultAppInsightPipeline : IPipelineFunctions
    {
        public bool Filter(ITelemetry evt)
        {
            return !(evt is MetricEvent);
        }

        public string GetName(ITelemetry evt)
        {
            return evt.Name;
        }

        public string GetUnit(ITelemetry evt)
        {
            throw new NotImplementedException();
        }

        public double GetValue(ITelemetry evt)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITelemetry> Transform(IEnumerable<ITelemetry> evts)
        {
              return evts
                    .OfType<MetricTelemetry>()
                    .GroupBy(e => new { e.Name })
                    .Select(e => new MetricTelemetry()
                    {
                        Name = e.Key.Name,
                        Value = e.Average(t => t.Value),
                        Timestamp = e.First().Timestamp,
                        Min = e.Min(t => t.Value),
                        Max = e.Max(t => t.Value),
                        Count = e.Count(),
                        StandardDeviation = Statistics.StandardDeviation(e.Select(t => t.Value)),

                        // TODO - how to add to a dictionary declaratively
                        //Statistics.Percentile(t => t.Value, 90)
                        //Statistics.Percentile(t => t.Value, 99)
                    });
        }
    }
}