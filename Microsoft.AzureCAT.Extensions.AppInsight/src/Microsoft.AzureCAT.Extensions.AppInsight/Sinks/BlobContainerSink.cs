using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AzureCAT.Extensions.AppInsight.Utils.Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.AzureCAT.Extensions.AppInsight.Sinks
{
    /// <summary>
    /// TODO - implement gzip compression for a blob chunk
    /// TODO - implement type demultiplexing (one type per blob) support 
    /// with broadcastblock and linkto with options
    /// </summary>
    public class BlobContainerSink : ITelemetryProcessor, IDisposable
    {
        private readonly ITelemetryProcessor _next;

        private readonly BufferBlock<ITelemetry> _bufferBlock;
        private TransformBlock<ITelemetry, byte[]> _transformBlock;        
        private ActionBlock<byte[]> _publishBlock;

        private readonly MemoryStream _memoryBuffer;

        private readonly CloudBlobContainer _container;
        private readonly Func<string> _blobPathFunc;
        private readonly Func<CloudBlockBlob, Task> _blobWrittenFunc;
        private System.IDisposable[] _disposables;

        public BlobContainerSink(ITelemetryProcessor next,
            CloudBlobContainer blobContainer,
            Func<string> blobPathFunc, 
            Func<CloudBlockBlob, Task> onBlobWrittenFunc, 
            int bufferSize = 4 * 1024 * 1024)
        {
            this._next = next;
            this._container = blobContainer;
            this._blobPathFunc = blobPathFunc;
            this._blobWrittenFunc = onBlobWrittenFunc;

            var transmitBuffer = new byte[bufferSize];
            _memoryBuffer = new MemoryStream(transmitBuffer);

            
            _bufferBlock = new BufferBlock<ITelemetry>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = 1024
                });
               
            _transformBlock = new TransformBlock<ITelemetry, byte[]>(
                evt => Transform(evt),
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 1,
                    MaxDegreeOfParallelism = 1
                });

            _publishBlock = new ActionBlock<byte[]>(
                async evts => await Publish(evts), 
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 16,
                    MaxDegreeOfParallelism = 1
                });

            _disposables = new IDisposable[]
            {
                _bufferBlock.LinkTo(_transformBlock),
                _transformBlock.LinkTo(_publishBlock)
            };

          
        }

        private byte[] Transform(ITelemetry evt)
        {
            var evts = new ITelemetry[] { evt };
            var strData = JsonSerializer
                .Serialize(evts, false);
            return strData;
        }

        private async Task Publish(byte[] evts)
        {
            try
            {
                if (_memoryBuffer.Length - _memoryBuffer.Position > evts.Length)
                    _memoryBuffer.Write(evts, 0, evts.Length);
                else
                {
                    // Flush the buffer
                    // TODO - more advanced version and iterate through a pool of buffers
                    await WriteBuffer(_memoryBuffer, 0, _memoryBuffer.Position);

                    // Clear the buffer and write
                    _memoryBuffer.Position = 0;
                    _memoryBuffer.Write(evts, 0, evts.Length);
                }
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.LogVerbose("Error in publishing to blob storage", ex.ToString());
            }            
        }

        private async Task WriteBuffer(Stream buffer, 
            int offset, long length)
        {
            var blobPath = _blobPathFunc();
            var blobReference = _container.GetBlockBlobReference(blobPath);
            _memoryBuffer.Position = 0;
            await blobReference
                .UploadFromStreamAsync(buffer, length)
                .ConfigureAwait(false);
            await _blobWrittenFunc(blobReference).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _memoryBuffer?.Dispose();   
        }

        public void Process(ITelemetry item)
        {
            _bufferBlock.Post(item);
            _next.Process(item);
        }
    }
}
