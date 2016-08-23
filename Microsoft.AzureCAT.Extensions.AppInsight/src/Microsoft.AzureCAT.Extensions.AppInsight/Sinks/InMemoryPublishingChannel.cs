using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AzureCAT.Extensions.AppInsight.Sinks
{
    /// <summary>
    /// Replacement for the standard AI publishing channel using TPL data flow for flow control
    /// and concurrency management
    /// </summary>
    public class InMemoryPublishingChannel : ITelemetryChannel, IDisposable
    {
        // Set the default endpoint address
        private Uri _endpointAddress = new Uri(Constants.TelemetryServiceEndpoint);
        private volatile bool _developerMode = false;
        private BatchingPublisher<ITelemetry> _pipeline;
        private BatchingPublisher<ITelemetry> _devPipeline;

        public InMemoryPublishingChannel()
            : this(500, TimeSpan.FromSeconds(30))
        { }

        public InMemoryPublishingChannel(int bufferSize, TimeSpan windowSize)
        {
            // Create the standard publishing channel
            _pipeline = new BatchingPublisher<ITelemetry>(bufferSize, windowSize,
                async (evts) => await PublishEvents(evts));

            // Create teh developer publishing channel
            _devPipeline = new BatchingPublisher<ITelemetry>(1, TimeSpan.Zero,
                async (evts) => await PublishEvents(evts));
        }

        /// <summary>
        /// Serializes a list of telemetry items and sends them.
        /// </summary>
        private async Task PublishEvents(IEnumerable<ITelemetry> telemetryItems)
        {
            if (telemetryItems == null || !telemetryItems.Any())
            {
                // CoreEventSource.Log.LogVerbose("No Telemetry Items passed to Enqueue");
                return;
            }

            byte[] data = JsonSerializer.Serialize(telemetryItems);
            var transmission = new Transmission(this._endpointAddress,
                data, "application/x-json-stream",
                JsonSerializer.CompressionType);

            await transmission.SendAsync().ConfigureAwait(false);
        }


        public bool? DeveloperMode
        {
            get { return _developerMode; }
            set
            {
                if (value.HasValue)
                    _developerMode = value.Value;
                else
                    _developerMode = false;
            }                           
        }


        /// <summary>
        ///  TODO - put a null guard on here
        /// </summary>
        protected Uri _EndpointAddress
        {
            get { return this._endpointAddress; }
            set { this._endpointAddress = value; }
        }

        public string EndpointAddress
        {
            get { return this._EndpointAddress.ToString(); }
            set { this._EndpointAddress = new Uri(value); }
        }

        public void Dispose()
        {
            _pipeline.Dispose();
            _devPipeline.Dispose();
        }

        public void Flush()
        {
            _devPipeline.Flush();
            _pipeline.Flush();
        }

        public void Send(ITelemetry item)
        {
            if (_developerMode)
                _devPipeline.Send(item);
            else
                _pipeline.Send(item);
        }
    }
}
