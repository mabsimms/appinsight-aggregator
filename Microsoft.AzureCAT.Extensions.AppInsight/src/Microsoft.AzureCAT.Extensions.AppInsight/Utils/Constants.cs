﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AzureCAT.Extensions.AppInsight
{
    /// <summary>
    /// These constants are copied out of the ai core client SDK, as they are following non-extensible
    /// coding practices
    /// </summary>
    internal class Constants
    {
        internal const string TelemetryServiceEndpoint = "https://dc.services.visualstudio.com/v2/track";

        internal const string TelemetryNamePrefix = "Microsoft.ApplicationInsights.";

        internal const string DevModeTelemetryNamePrefix = "Microsoft.ApplicationInsights.Dev.";

        // This GUID was generated from the string 'Microsoft-ApplicationInsights'.
        internal const string TelemetryGroup = "{0d943590-b235-5bdb-f854-89520f32fc0b}";

        // This GUID was generated from the string 'Microsoft-ApplicationInsights-Dev'.
        internal const string DevModeTelemetryGroup = "{ba84f32b-8af2-5006-f147-5030cdd7f22d}";

        // This is a special EventSource key for groups and cannot be changed.
        internal const string EventSourceGroupTraitKey = "ETW_GROUP";

        internal const int MaxExceptionCountToSave = 10;

        internal const double DefaultSamplingPercentage = 100.0;
    }
}
