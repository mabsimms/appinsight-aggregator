using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Channel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace AzureCAT.Samples.AppInsight
{
    public class GraphitePublisher : ITelemetryProcessor, System.IDisposable
    {
        protected readonly ITelemetryProcessor _next;
        protected readonly string _host;
        protected readonly int _port;
        protected readonly ILogger _logger;
        protected readonly BatchBlock<ITelemetry> _batchBlock;
        protected readonly ActionBlock<IEnumerable<ITelemetry>> _actionBlock;
        protected readonly CancellationTokenSource _tokenSource;
        private System.Threading.Timer _windowTimer;
        private System.IDisposable[] _disposables;

        public GraphitePublisher(ITelemetryProcessor next,
            ILogger logger,
            string hostName,
            int port,
            System.TimeSpan maxFlushTime,
            int maxWindowEventCount = 100)
        {
            this._next = next;
            this._host = hostName;
            this._port = port;
            this._logger = logger;
            this._tokenSource = new CancellationTokenSource();

            this._batchBlock = new BatchBlock<ITelemetry>(maxWindowEventCount,
                new GroupingDataflowBlockOptions()
                {
                    BoundedCapacity = maxWindowEventCount,
                    Greedy = true,
                    CancellationToken = _tokenSource.Token
                });

           this._actionBlock = new ActionBlock<IEnumerable<ITelemetry>>(
                async (e) => await Publish(e),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 1,
                    BoundedCapacity = 2,
                    CancellationToken = _tokenSource.Token
                });

                var disp = new List<System.IDisposable>();
                disp.Add(_batchBlock.LinkTo(_actionBlock));
                _disposables = disp.ToArray();

                this._windowTimer = new Timer(FlushBuffer, null, 
                    maxFlushTime, maxFlushTime);
        }

        protected void FlushBuffer(object state)
        {
            if (_batchBlock != null)
            {
                _batchBlock.TriggerBatch();
            }
        }
         
        public void Process(ITelemetry item)
        {
            this._batchBlock.Post(item);
            this._next.Process(item);
        }

        protected async Task Publish(IEnumerable<ITelemetry> events)
        {
            if (events == null)
                return;
            try
            {
                var eventsList = events.ToArray();
                if (eventsList.Length == 0)
                    return;

                var content = GetGraphiteContent(eventsList); 
                await PublishEvents(content);  
            }
            catch (System.Exception ex)
            {
                _logger.LogError(0, ex, "error in processing {count} events", events?.Count());
            }
        }

        protected async Task PublishEvents(IList<string> events)
        {
            using (var tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(_host, _port);
                using (var stream = tcpClient.GetStream())
                using (var sw = new StreamWriter(stream))
                {
                    foreach (var e in events)
                        await sw.WriteLineAsync(e);
                }                  
            }
        }

        public IList<string> GetGraphiteContent(IEnumerable<ITelemetry> eventsList)
        {
            var contentList = new List<string>();
            foreach (var e in eventsList)
            {
                if (e is MetricTelemetry)
                {
                    var me = e as MetricTelemetry;
                    contentList.Add($"{me.Name}.avg {me.Value} {me.Timestamp.ToUnixTimeSeconds()}");
                    contentList.Add($"{me.Name}.min {me.Min} {me.Timestamp.ToUnixTimeSeconds()}");
                    contentList.Add($"{me.Name}.max {me.Max} {me.Timestamp.ToUnixTimeSeconds()}");
                    contentList.Add($"{me.Name}.count {me.Count} {me.Timestamp.ToUnixTimeSeconds()}");
                    contentList.Add($"{me.Name}.stddev {me.StandardDeviation} {me.Timestamp.ToUnixTimeSeconds()}");

                    if (me.Properties.ContainsKey("P50"))
                        contentList.Add($"{me.Name}.p50 {me.Properties["P50"]} {me.Timestamp.ToUnixTimeSeconds()}\n");
                    if (me.Properties.ContainsKey("P90"))
                        contentList.Add($"{me.Name}.p90 {me.Properties["P90"]} {me.Timestamp.ToUnixTimeSeconds()}\n");
                   if (me.Properties.ContainsKey("P99"))
                        contentList.Add($"{me.Name}.p99 {me.Properties["P99"]} {me.Timestamp.ToUnixTimeSeconds()}\n");
                }
            }
            return contentList;
        }

        public void Dispose()
        {
            _tokenSource.Cancel();

            if (_disposables != null) 
            {
               foreach (var d in _disposables)
                   d.Dispose();
               _disposables = null;
            }

            if (_windowTimer != null)
            {
                _windowTimer.Dispose();
                _windowTimer = null;
            }
        }
    }
}