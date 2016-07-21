using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace AzureCAT.Samples.AppInsight.Aggregator
{
    public class DefaultClientTransforms : IPipelineFunctions
    {
        public bool Filter(ITelemetry evt)
        {
            return !(evt is MetricTelemetry);
        }

        public string GetName(ITelemetry evt)
        {
            return (evt as MetricTelemetry)?.Name;
        }

        public string GetUnit(ITelemetry evt)
        {
            throw new NotImplementedException();
        }

        public double GetValue(ITelemetry evt)
        {
            return (evt as MetricTelemetry)?.Value ?? 0.0;
        }
    }
}
