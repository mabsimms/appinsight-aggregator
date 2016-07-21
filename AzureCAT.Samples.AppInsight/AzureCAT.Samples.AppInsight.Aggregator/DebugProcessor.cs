using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace AzureCAT.Samples.AppInsight.Aggregator
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
            Console.WriteLine(item.ToString());
            this._next.Process(item);    
        }
    }
}
