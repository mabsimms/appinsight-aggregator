namespace AzureCAT.Samples.AppInsight
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using System;
    using Microsoft.Extensions.Logging;

    public class TelemetryAggregator :
        SlidingWindowBase<ITelemetry, ITelemetry>, 
        ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;
        private readonly bool _publishRawEvents;

        public TelemetryAggregator(ITelemetryProcessor next, 
            ILogger logger,
            IPipelineFunctions funcs,
            TimeSpan windowSpan,
            bool publishRawEvents,
            int maxEventBacklog = 10000,
            int maxEventWindowCount = 10000)
            : base(new SlidingWindowConfiguration() { 
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
                        catch (Exception ex) {
							logger.LogError(0, ex, "Error in telemetry pipeline");
                        }
                    }
                    return Task.FromResult(0); 
                }, logger)
        {
            this._next = next;
            this._publishRawEvents = publishRawEvents;
        } 

        public void Process(ITelemetry item)
        {
            this.Enqueue(item);
            if (_publishRawEvents)
                this._next.Process(item);
        }
    }
}
