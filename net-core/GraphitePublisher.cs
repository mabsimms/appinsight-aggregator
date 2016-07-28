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

namespace AzureCAT.Samples.AppInsight
{
    public class GraphitePublisher : ITelemetryProcessor
    {
        protected readonly ITelemetryProcessor _next;
        protected readonly string _host;
        protected readonly int _port;
        
        protected BatchBlock<ITelemetry> _batchBlock;
        protected ActionBlock<IEnumerable<ITelemetry>> _actionBlock;

        protected CancellationTokenSource _tokenSource;
        private System.Threading.Timer _windowTimer;

        public GraphitePublisher(ITelemetryProcessor next,
            string hostName,
            int port,
            System.TimeSpan maxFlushTime,
            int maxWindowEventCount = 100)
        {
            this._next = next;
            this._host = hostName;
            this._port = port;
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
                // TODO - handle disposal    

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
            {
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
                    // TODO - internal error log
                }
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
                    //contentList.Add($"{me.Name}.p50 {me.} {me.Timestamp.ToUnixTimeSeconds()}\n");
                    //contentList.Add($"{me.Name}.p90 {me.Value} {me.Timestamp.ToUnixTimeSeconds()}\n");
                    //contentList.Add($"{me.Name}.p99 {me.Value} {me.Timestamp.ToUnixTimeSeconds()}\n");
                }
            }
            return contentList;
        }
/*
  public async Task SendMetric(IEnumerable<MetricEvent> evts)
                // Send each metric as a set of individual gauges or counters (separated by newlines)
                // metric_path value timestamp\n
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(_hostName, _port);
                    using (var stream = tcpClient.GetStream())
                    using (var sw = new StreamWriter(stream))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var evt in evts)
                        {
                            foreach (var kv in metricMap)
                            {
                                sb.Append(evt.MetricName);
                                sb.Append(kv.Key);
                                sb.Append(' ');
                                sb.Append(metricMap[kv.Key](evt));
                                sb.Append(" ");
                                sb.Append(evt.Timestamp.ToUnixTimeSeconds());
                                await sw.WriteLineAsync(sb.ToString());
                                sb.Clear();
                            }                          
                        }                        
                    }
                }
            }
            catch (Exception ex0)
            {
                // In the case of failure log the error count and continue.  These are
                // continuous values, so the next value will replace anyway
                System.Diagnostics.Trace.WriteLine("Error in graphite publisher: " + 
                    ex0.ToString());
            }
        }*/



    }
}