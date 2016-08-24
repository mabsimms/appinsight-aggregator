using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AzureCAT.Extensions.AppInsight.Utils
{
    namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
    {
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if CORE_PCL || NET45 || NET46
    using System.Diagnostics.Tracing;
#endif

        [EventSource(Name = "Microsoft-ApplicationInsights-Core")]
        internal sealed class CoreEventSource : EventSource
        {
            public static readonly CoreEventSource Log = new CoreEventSource();

            private static readonly string name = "";

            public bool IsVerboseEnabled
            {
                [NonEvent]
                get
                {
                    return Log.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
                }
            }

            /// <summary>
            /// Logs the information when there operation to track is null.
            /// </summary>
            [Event(1, Message = "Operation object is null.", Level = EventLevel.Warning)]
            public void OperationIsNullWarning(string appDomainName = "Incorrect")
            {
                this.WriteEvent(1, name);
            }

            /// <summary>
            /// Logs the information when there operation to stop does not match the current operation.
            /// </summary>
            [Event(2, Message = "Operation to stop does not match the current operation.", Level = EventLevel.Error)]
            public void InvalidOperationToStopError(string appDomainName = "Incorrect")
            {
                this.WriteEvent(2, name);
            }

            [Event(
                3,
                Keywords = Keywords.VerboseFailure,
                Message = "[msg=Log verbose];[msg={0}]",
                Level = EventLevel.Verbose)]
            public void LogVerbose(string msg, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    3,
                    msg ?? string.Empty,
                    name);
            }

            [Event(
                4,
                Keywords = Keywords.Diagnostics | Keywords.UserActionable,
                Message = "Diagnostics event throttling has been started for the event {0}",
                Level = EventLevel.Informational)]
            public void DiagnosticsEventThrottlingHasBeenStartedForTheEvent(
                string eventId,
                string appDomainName = "Incorrect")
            {
                this.WriteEvent(4, eventId ?? "NULL", name);
            }

            [Event(
                5,
                Keywords = Keywords.Diagnostics | Keywords.UserActionable,
                Message = "Diagnostics event throttling has been reset for the event {0}, event was fired {1} times during last interval",
                Level = EventLevel.Informational)]
            public void DiagnosticsEventThrottlingHasBeenResetForTheEvent(
                int eventId,
                int executionCount,
                string appDomainName = "Incorrect")
            {
                this.WriteEvent(5, eventId, executionCount, name);
            }

            [Event(
                6,
                Keywords = Keywords.Diagnostics,
                Message = "Scheduler timer dispose failure: {0}",
                Level = EventLevel.Warning)]
            public void DiagnoisticsEventThrottlingSchedulerDisposeTimerFailure(
                string exception,
                string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    6,
                    exception ?? string.Empty,
                    name);
            }

            [Event(
                7,
                Keywords = Keywords.Diagnostics,
                Message = "A scheduler timer was created for the interval: {0}",
                Level = EventLevel.Verbose)]
            public void DiagnoisticsEventThrottlingSchedulerTimerWasCreated(
                string intervalInMilliseconds,
                string appDomainName = "Incorrect")
            {
                this.WriteEvent(7, intervalInMilliseconds ?? "NULL", name);
            }

            [Event(
                8,
                Keywords = Keywords.Diagnostics,
                Message = "A scheduler timer was removed",
                Level = EventLevel.Verbose)]
            public void DiagnoisticsEventThrottlingSchedulerTimerWasRemoved(string appDomainName = "Incorrect")
            {
                this.WriteEvent(8, name);
            }

            [Event(
                9,
                Message = "No Telemetry Configuration provided. Using the default TelemetryConfiguration.Active.",
                Level = EventLevel.Warning)]
            public void TelemetryClientConstructorWithNoTelemetryConfiguration(string appDomainName = "Incorrect")
            {
                this.WriteEvent(9, name);
            }

            [Event(
                10,
                Message = "Value for property '{0}' of {1} was not found. Populating it by default.",
                Level = EventLevel.Verbose)]
            public void PopulateRequiredStringWithValue(string parameterName, string telemetryType, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    10,
                    parameterName ?? string.Empty,
                    telemetryType ?? string.Empty,
                    name);
            }

            [Event(
                11,
                Message = "Invalid duration for Request Telemetry. Setting it to '00:00:00'.",
                Level = EventLevel.Warning)]
            public void RequestTelemetryIncorrectDuration(string appDomainName = "Incorrect")
            {
                this.WriteEvent(11, name);
            }

            [Event(
               12,
               Message = "Telemetry tracking was disabled. Message is dropped.",
               Level = EventLevel.Verbose)]
            public void TrackingWasDisabled(string appDomainName = "Incorrect")
            {
                this.WriteEvent(12, name);
            }

            [Event(
               13,
               Message = "Telemetry tracking was enabled. Messages are being logged.",
               Level = EventLevel.Verbose)]
            public void TrackingWasEnabled(string appDomainName = "Incorrect")
            {
                this.WriteEvent(13, name);
            }

            [Event(
                14,
                Keywords = Keywords.ErrorFailure,
                Message = "[msg=Log Error];[msg={0}]",
                Level = EventLevel.Error)]
            public void LogError(string msg, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    14,
                    msg ?? string.Empty,
                    name);
            }

            [Event(
                15,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. Type '{0}' was not found. Type loading was skipped. Monitoring will continue.",
                Level = EventLevel.Error)]
            public void TypeWasNotFoundConfigurationError(string type, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    15,
                    type ?? string.Empty,
                    name);
            }

            [Event(
                16,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. Type '{0}' does not implement '{1}'. Type loading was skipped. Monitoring will continue.",
                Level = EventLevel.Error)]
            public void IncorrectTypeConfigurationError(string type, string expectedType, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    16,
                    type ?? string.Empty,
                    expectedType ?? string.Empty,
                    name);
            }

            [Event(
                17,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. Type '{0}' does not have property '{1}'. Property initialization was skipped. Monitoring will continue.",
                Level = EventLevel.Error)]
            public void IncorrectPropertyConfigurationError(string type, string property, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    17,
                    type ?? string.Empty,
                    property ?? string.Empty,
                    name);
            }

            [Event(
                18,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. Element '{0}' element does not have a Type attribute, does not specify a value and is not a valid collection type. Type initialization was skipped. Monitoring will continue.",
                Level = EventLevel.Error)]
            public void IncorrectInstanceAtributesConfigurationError(string definition, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    18,
                    definition ?? string.Empty,
                    name);
            }

            [Event(
                19,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. '{0}' element has unexpected contents: '{1}': '{2}'. Type initialization was skipped. Monitoring will continue.",
                Level = EventLevel.Error)]
            public void LoadInstanceFromValueConfigurationError(string element, string contents, string error, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    19,
                    element ?? string.Empty,
                    contents ?? string.Empty,
                    error ?? string.Empty,
                    name);
            }

            [Event(
                20,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. Exception: '{0}'. Monitoring will continue if you set InsrumentationKey programmatically.",
                Level = EventLevel.Error)]
            public void ConfigurationFileCouldNotBeParsedError(string error, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    20,
                    error ?? string.Empty,
                    name);
            }

            [Event(
                21,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. Type '{0}' will not be create. Error: '{1}'. Monitoring will continue if you set InsrumentationKey programmatically.",
                Level = EventLevel.Error)]
            public void MissingMethodExceptionConfigurationError(string type, string error, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    21,
                    type ?? string.Empty,
                    error ?? string.Empty,
                    name);
            }

            [Event(
                22,
                Keywords = Keywords.UserActionable,
                Message = "ApplicationInsights configuration file loading failed. Type '{0}' will not be initialized. Error: '{1}'. Monitoring will continue if you set InsrumentationKey programmatically.",
                Level = EventLevel.Error)]
            public void ComponentInitializationConfigurationError(string type, string error, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    22,
                    type ?? string.Empty,
                    error ?? string.Empty,
                    name);
            }

            [Event(
                23,
                Message = "ApplicationInsights configuration file '{0}' was not found.",
                Level = EventLevel.Warning)]
            public void ApplicationInsightsConfigNotFoundWarning(string file, string appDomainName = "Incorrect")
            {
                this.WriteEvent(
                    23,
                    file ?? string.Empty,
                    name);
            }

            /// <summary>
            /// Keywords for the PlatformEventSource.
            /// </summary>
            public sealed class Keywords
            {
                /// <summary>
                /// Key word for user actionable events.
                /// </summary>
                public const EventKeywords UserActionable = (EventKeywords)EventSourceKeywords.UserActionable;

                /// <summary>
                /// Keyword for errors that trace at Verbose level.
                /// </summary>
                public const EventKeywords Diagnostics = (EventKeywords)EventSourceKeywords.Diagnostics;

                /// <summary>
                /// Keyword for errors that trace at Verbose level.
                /// </summary>
                public const EventKeywords VerboseFailure = (EventKeywords)EventSourceKeywords.VerboseFailure;

                /// <summary>
                /// Keyword for errors that trace at Error level.
                /// </summary>
                public const EventKeywords ErrorFailure = (EventKeywords)EventSourceKeywords.ErrorFailure;
            }
        }
    }

    internal static class EventSourceKeywords
    {
        public const long UserActionable = 0x1;

        public const long Diagnostics = 0x2;

        public const long VerboseFailure = 0x4;

        public const long ErrorFailure = 0x8;

        public const long ReservedUserKeywordBegin = 0x10;
    }

}
