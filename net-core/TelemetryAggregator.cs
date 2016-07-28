namespace AzureCAT.Samples.AppInsight
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using System;

    public class TelemetryAggregator :
        SlidingWindowBase<ITelemetry, ITelemetry>, 
        ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;
        private readonly bool _publishRawEvents;

        public TelemetryAggregator(ITelemetryProcessor next, 
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
                    // TODO - catch any errors here
                    foreach (var e in evts) next.Process(e);
                    return Task.FromResult(0); 
                })
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