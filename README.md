# AppInsight Client SDK aggregator

Example of using TPL data flow to perform in-memory aggregation for observed telemetry events (currently only 
supports MetricEvent type) with some ability to plug-in filtering logic.

TODOs:
- Support all ITelemetry event types (e.g. track events)
- Add the concurrency guard to prevent name type explosion
- Add a name consolidator function/transform (to prevent thigns like /a/b/c/guid,guid,guid from filling up the downstream pipe)
- Add a servertelemetry telemetry publisher using the same producer/consumer queue based on TPL data flow

