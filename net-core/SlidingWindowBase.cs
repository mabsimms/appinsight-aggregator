namespace AzureCAT.Samples.AppInsight
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using System.Threading;
    using Microsoft.Extensions.Configuration;

    public class SlidingWindowBase<TInput, TOutput> : IDisposable
    {
        private readonly ILogger _logger;

        private BufferBlock<TInput> _buffer;
        private BatchBlock<TInput> _batcher;

        private TransformBlock<IEnumerable<TInput>, IEnumerable<TOutput>> _aggregator;
        private ActionBlock<IEnumerable<TOutput>> _publisher;

        private CancellationTokenSource _tokenSource;
        private System.Threading.Timer _windowTimer;
        private IDisposable[] _disposables;

        private readonly Func<TInput, bool> _filterFunc;
        private readonly Func<TInput, string> _nameFunc;
        private readonly Func<IEnumerable<TInput>, IEnumerable<TOutput>> _transformFunc;
        private readonly Func<IEnumerable<TOutput>, Task> _publishFunc;

        private long _droppedEvents;

        public SlidingWindowBase(IConfiguration config, ILogger log,
            Func<TInput, bool> filterFunc,
            Func<TInput, string> nameFunc,
            Func<IEnumerable<TInput>, IEnumerable<TOutput>> transformFunc,
            Func<IEnumerable<TOutput>, Task> publishFunc)
        {
            _tokenSource = new CancellationTokenSource();
            _logger = log;

            // Set up the message transforms
            this._filterFunc = filterFunc;
            this._publishFunc = publishFunc;
            this._transformFunc = transformFunc;
            this._nameFunc = nameFunc;

            // Set up the publisher - publish back into the pipeline
            InitializeFlow(config);
        }

        protected void InitializeFlow(IConfiguration config)
        {
            // TODO - use the extension method
            int maxBacklog = Int16.Parse(config["maxBacklog"]);
            int maxWindowEventCount = Int32.Parse(config["maxWindowCount"]);
            TimeSpan windowSize = TimeSpan.Parse(config["windowSize"]);

            var bufferOptions = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = maxBacklog,
                CancellationToken = _tokenSource.Token
            };
            _buffer = new BufferBlock<TInput>(bufferOptions);

            _batcher = new BatchBlock<TInput>(maxWindowEventCount,
                new GroupingDataflowBlockOptions()
                {
                    BoundedCapacity = maxWindowEventCount,
                    Greedy = true,
                    CancellationToken = _tokenSource.Token
                });

            _aggregator = new TransformBlock<IEnumerable<TInput>, IEnumerable<TOutput>>(
                transform: (e) => _transformFunc(e),
                dataflowBlockOptions: new ExecutionDataflowBlockOptions()
                {
                    CancellationToken = _tokenSource.Token
                });

            _publisher = new ActionBlock<IEnumerable<TOutput>>(
                async (e) => await _publishFunc(e),
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

            this._windowTimer = new Timer(FlushBuffer, null, windowSize, windowSize);
        }

        private void FlushBuffer(object state)
        {
            if (_batcher != null)
            {
                _batcher.TriggerBatch();
            }
        }

        public void Process(TInput item)
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
        }

        protected async Task Publish(IEnumerable<TOutput> events)
        {
            try
            {
                await _publishFunc(events);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in publishing {count} messages", events.Count());
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