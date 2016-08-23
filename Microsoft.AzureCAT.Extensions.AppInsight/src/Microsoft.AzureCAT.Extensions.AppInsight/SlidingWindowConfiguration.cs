using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AzureCAT.Extensions.AppInsight
{
    public class SlidingWindowConfiguration
    {
        public int MaxBacklogSize { get; set; }
        public int MaxWindowEventCount { get; set; }
        public TimeSpan SlidingWindowSize { get; set; }
    }
}
