using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AzureCAT.Extensions.AppInsight
{    
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using System;
    using Microsoft.Extensions.Logging;

    public class InMemoryAggregator :
        SlidingWindowBase<ITelemetry, ITelemetry>,
        ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;
        private volatile bool _publishRawEvents;

        public InMemoryAggregator(ITelemetryProcessor next,
            ILogger logger,
            IPipelineFunctions funcs,
            TimeSpan windowSpan,
            bool publishRawEvents = true,
            int maxEventBacklog = 10000,
            int maxEventWindowCount = 500)
            : base(new SlidingWindowConfiguration()
            {
                MaxBacklogSize = maxEventBacklog,
                MaxWindowEventCount = maxEventWindowCount,
                SlidingWindowSize = windowSpan
            },
                funcs.Filter,
                funcs.GetName,
                funcs.Transform,
                (evts) => {
                    if (evts == null)
                        return Task.FromResult(0);
                    foreach (var e in evts)
                    {
                    // TODO - do something better than on error goto next
                    try
                        {
                            next.Process(e);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(0, ex, "Error in telemetry pipeline");
                        }
                    }
                    return Task.FromResult(0);
                }, logger)
        {
            this._next = next;
            this._publishRawEvents = publishRawEvents;
        }

        public bool PublishRawEvents {
            get { return _publishRawEvents; }
            set { _publishRawEvents = value;  }
        }

        public void Process(ITelemetry item)
        {
            this.Enqueue(item);
            if (_publishRawEvents)
                this._next.Process(item);
        }
    }

    public interface IPipelineFunctions
    {
        bool Filter(ITelemetry evt);
        string GetName(ITelemetry evt);
        IEnumerable<ITelemetry> Transform(IEnumerable<ITelemetry> evts);
    }


}
