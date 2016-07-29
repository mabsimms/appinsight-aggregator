# AppInsight Client SDK aggregator

Example of using TPL data flow to perform in-memory aggregation for observed telemetry events (currently only 
supports MetricEvent type) with some ability to plug-in filtering logic.

## AzureCAT.Samples.AppInsight

First cut sample using .NET Framework 4.5.  Deprecated.

## net-core

More robust implementation built on top of .NET Core 1.0, and compatible with Linux/MacOS execution.  Open items and
TODOs:

- [ ] Support non-MetricTelemetry events
- [ ] Add internal error logging
- [ ] Add internal MetricEvents for tracking dropped/throttled events, etc
