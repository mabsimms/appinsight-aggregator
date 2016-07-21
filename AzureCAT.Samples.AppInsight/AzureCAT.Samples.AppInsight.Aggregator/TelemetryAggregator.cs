
namespace AzureCAT.Samples.AppInsight.Aggregator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using System.Threading;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using MathNet.Numerics.Statistics;

    
    public class TelemetryAggregator : ITelemetryProcessor, IDisposable
    {
        private BufferBlock<ITelemetry> _buffer;
        private BatchBlock<ITelemetry> _batcher;

        private TransformBlock<IEnumerable<ITelemetry>, IEnumerable<ITelemetry>> _aggregator;
        private ActionBlock<IEnumerable<ITelemetry>> _publisher;

        private CancellationTokenSource _tokenSource;
        private System.Threading.Timer _windowTimer;
        private IDisposable[] _disposables;

        private readonly Func<ITelemetry, bool> _filterFunc;
        private readonly Func<ITelemetry, string> _nameFunc;
        private readonly Func<ITelemetry, string> _unitFunc;
        private readonly Func<ITelemetry, double> _valueFunc;
        private readonly Func<IEnumerable<ITelemetry>, Task> _publishFunc;

        private long _droppedEvents;

        private readonly ITelemetryProcessor _next;

        public TelemetryAggregator(ITelemetryProcessor next,
            TelemetryAggregatorConfig config)
        {
            _next = next;

            _tokenSource = new CancellationTokenSource();

            // Set up the message transforms
            IPipelineFunctions pipelineFunctions = new DefaultClientTransforms();
            this._filterFunc = pipelineFunctions.Filter;
            this._nameFunc = pipelineFunctions.GetName;
            this._unitFunc = pipelineFunctions.GetUnit;
            this._valueFunc = pipelineFunctions.GetValue;

            // Set up the publisher - publish back into the pipeline
            _publishFunc = (evts) => {
                foreach (var e in evts) _next.Process(e);
                return Task.FromResult(0);
            };

            InitializeFlow(config);
        }

        protected void InitializeFlow(TelemetryAggregatorConfig config)
        {
            var bufferOptions = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = config.MaxBacklog,
                CancellationToken = _tokenSource.Token
            };
            _buffer = new BufferBlock<ITelemetry>(bufferOptions);

            _batcher = new BatchBlock<ITelemetry>(config.MaxWindowEventCount,
                new GroupingDataflowBlockOptions()
                {
                    BoundedCapacity = config.MaxWindowEventCount,
                    Greedy = true,
                    CancellationToken = _tokenSource.Token
                });

            _aggregator = new TransformBlock<IEnumerable<ITelemetry>, IEnumerable<ITelemetry>>(
                transform: (e) => Aggregate(e),
                dataflowBlockOptions: new ExecutionDataflowBlockOptions()
                {
                    CancellationToken = _tokenSource.Token
                });

            _publisher = new ActionBlock<IEnumerable<ITelemetry>>(
                async (e) => await Publish(e),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 1,
                    BoundedCapacity = 32,
                    CancellationToken = _tokenSource.Token
                });

            var disp = new List<IDisposable>();
            disp.Add(_buffer.LinkTo(_batcher));
            disp.Add(_batcher.LinkTo(_aggregator));
            disp.Add(_aggregator.LinkTo(_publisher));
            _disposables = disp.ToArray();

            this._windowTimer = new Timer(FlushBuffer, null, config.Window, config.Window);
        }

        private void FlushBuffer(object state)
        {
            if (_batcher != null)
            {
                _batcher.TriggerBatch();
            }
        }

        public void Process(ITelemetry item)
        {
            // Do we process this record for local aggregation?                
            if (!_filterFunc(item))
            {
                if (!_buffer.Post(item))
                {
                    // Increase the number of dropped events
                    Interlocked.Increment(ref _droppedEvents);
                }
                    
                // TODO - do we "eat" aggregated messages?
                return;
            }

            _next.Process(item);
        }


        protected IEnumerable<ITelemetry> Aggregate(IEnumerable<ITelemetry> events)
        {
            try
            {
                return events                 
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
            catch (Exception ex)
            {
                Trace.WriteLine("Error in aggregation: " + ex.ToString());
                return Enumerable.Empty<ITelemetry>();
            }
        }

        protected async Task Publish(IEnumerable<ITelemetry> events)
        {
            try
            {
                await _publishFunc(events);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error in publish function: " + ex.ToString());
            }
        }

        public void Dispose()
        {
            _windowTimer.Dispose();

            _tokenSource.Cancel();
            _buffer.Completion.Wait();
            _batcher.Completion.Wait();
            _aggregator.Completion.Wait();
            _publisher.Completion.Wait();

            foreach (var d in _disposables)
                d.Dispose();
        }
    }

        
    
}
