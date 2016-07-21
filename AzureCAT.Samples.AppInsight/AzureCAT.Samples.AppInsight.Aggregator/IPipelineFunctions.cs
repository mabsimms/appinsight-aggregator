using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCAT.Samples.AppInsight.Aggregator
{
    public interface IPipelineFunctions
    {
        bool Filter(ITelemetry evt);
        string GetName(ITelemetry evt);
        string GetUnit(ITelemetry evt);
        double GetValue(ITelemetry evt);
    }
}
