namespace AzureCAT.Samples.AppInsight
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class TelemetryAggregator :
        SlidingWindowBase<ITelemetry, ITelemetry>, 
        ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public TelemetryAggregator(ITelemetryProcessor next, 
            IPipelineFunctions funcs)
            : base(null, null, 
                funcs.Filter, 
                funcs.GetName, 
                funcs.Transform, 
                (evts) => { 
                    foreach (var e in evts) next.Process(e);
                    return Task.FromResult(0); 
                })
        {

        } 
    }

    public interface IPipelineFunctions
    {
        bool Filter(ITelemetry evt);
        string GetName(ITelemetry evt);
        string GetUnit(ITelemetry evt);
        double GetValue(ITelemetry evt);

        IEnumerable<ITelemetry> Transform(IEnumerable<ITelemetry> evts);

        Task Publish(IEnumerable<ITelemetry> evts);
    }

    public interface ITelemetry {}
    public interface ITelemetryProcessor {
        void Process(ITelemetry evt);

    }
}