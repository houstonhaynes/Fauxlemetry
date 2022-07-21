# Fauxlemetry

This console app will generate delimited data with some correlation between values. The objective is primarily to seed a starting record set for a Redis instance that will be the data plane for the project. Initially the data will be loaded in batch/stream to a local Redis Stack instance.

## Faster Than Realtime Data Generation

The objective is to generate a volume of data that has certain "event rates" for combinations of values. Because there's a business scenario that dictates these events occur with a certain incident rate and span-of-time, there needs to be a specific time stamp associated with them that makes a form of business sense. For now there will be a day-wide distribution of values, but eventually other data "shapes" will be contemplated - such as "peak" activity times and event patterns that span multiple days.

## Real Time Data

Once that initial 30-day corpus of data is generated, then the console will be used to emit streams of data that simulates real time traffic as though it comes from the originating system that would ostensibly produce these events if the Redis instance was connected to a project system.

## Origins

This repository is based on the template generated from the @EluciusFTW 's [FSharp Spectre.Console Template](https://github.com/EluciusFTW/fsharp-spectre-console-template). See that and the Spectre.Console site for more information.
