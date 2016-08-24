using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AzureCAT.Extensions.AppInsight.Utils.Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

namespace Microsoft.AzureCAT.Extensions.AppInsight
{
    public class BatchingPublisher<T> : IDisposable
    {
        // TPL Dataflow pipeline objects and lifecycle management via CancellationToken
        private BufferBlock<T> _buffer;
        private BatchBlock<T> _batcher;
        private ActionBlock<IEnumerable<T>> _publish;

        private readonly CancellationTokenSource _tokenSource;
        private IDisposable[] _disposables;
        private int _disposeCount = 0;

        // Background timer to periodically flush the batch block
        private System.Threading.Timer _windowTimer;
        private readonly Func<IEnumerable<T>, Task> _publishFunc;

        public BatchingPublisher(int capacity, TimeSpan windowSize,
            Func<IEnumerable<T>, Task> publishFunc)
        {
            this._publishFunc = publishFunc;
            this._tokenSource = new CancellationTokenSource();

            // Starting the Runner
            InitializePipeline(capacity, windowSize);
        }

        private void InitializePipeline(int maxBufferedCapacity,
            TimeSpan windowSize)
        {
            _buffer = new BufferBlock<T>(
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = maxBufferedCapacity * 2,
                    CancellationToken = _tokenSource.Token
                });

            _batcher = new BatchBlock<T>(maxBufferedCapacity,
                new GroupingDataflowBlockOptions()
                {
                    BoundedCapacity = maxBufferedCapacity,
                    Greedy = true,
                    CancellationToken = _tokenSource.Token
                });

            _publish = new ActionBlock<IEnumerable<T>>(
                async (e) => await _publishFunc(e),
                   new ExecutionDataflowBlockOptions()
                   {
                       // Maximum of one concurrent batch being published
                       MaxDegreeOfParallelism = 1,

                       // Maximum of three pending batches to be published
                       BoundedCapacity = 3,
                       CancellationToken = _tokenSource.Token
                   });

            _disposables = new IDisposable[]
            {
                _buffer.LinkTo(_batcher),
                _batcher.LinkTo(_publish)
            };

            _windowTimer = new Timer(Flush, null,
               windowSize, windowSize);
            
        }

        /// <summary>
        /// Flushes the in-memory buffer and sends it.
        /// </summary>
        public void Flush(object state = null)
        {
            _batcher?.TriggerBatch();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
                _tokenSource.Cancel();
                _windowTimer?.Dispose();
                foreach (var d in _disposables)
                    d.Dispose();
            }
        }

        public void Send(T item)
        {
            try
            {
                if (!_buffer.Post(item))
                {
                    // TODO; immediate flush?
                }
            }
            catch (Exception e)
            {                
                CoreEventSource.Log.LogVerbose("PipelinedInMemoryTransmitter.Enqueue failed: ", e.ToString());
            }
        }
    }
}
