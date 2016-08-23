# AppInsight Client SDK aggregator

Example of using TPL data flow to perform in-memory aggregation for observed telemetry events (currently only 
supports MetricEvent type) with some ability to plug-in filtering logic.

## Microsoft.AzureCAT.Extensions.AppInsights

Implementation built on top of .NET Core 1.0, and compatible with Linux/MacOS execution. Also includes TPL Data Flow
based publishing channel. Open items and
TODOs:

- [ ] Support non-MetricTelemetry events
- [ ] Filtering and aggregation of standard asp.net/ado.net/etc events
- [ ] Add internal error logging
- [ ] Add internal MetricEvents for tracking dropped/throttled events, etc
