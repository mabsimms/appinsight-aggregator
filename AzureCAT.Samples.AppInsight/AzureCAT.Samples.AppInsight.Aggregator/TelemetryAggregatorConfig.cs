using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCAT.Samples.AppInsight.Aggregator
{
    public class TelemetryAggregatorConfig
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(1);
        public int MaxBacklog { get; set; } = 10000;
        public int MaxWindowEventCount { get; set; } = 20000;
        public PublishTarget Target { get; set; } = PublishTarget.Self;
    }
    
    public enum PublishTarget
    {
        Self,
        Null
    }
}
