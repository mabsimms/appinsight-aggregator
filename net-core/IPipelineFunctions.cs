using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace AzureCAT
{
  public interface IPipelineFunctions
    {
        bool Filter(ITelemetry evt);
        string GetName(ITelemetry evt);
        IEnumerable<ITelemetry> Transform(IEnumerable<ITelemetry> evts);
    }
}
