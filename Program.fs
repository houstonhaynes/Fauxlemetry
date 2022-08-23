open Spectre.Console.Cli
open Commands

[<EntryPoint>]
let main argv =

    let app = CommandApp()

    app.Configure (fun config ->

        config
            .AddCommand<TimeSeries.Emit>("drip")
            .WithAlias("d")
            .WithDescription("Creates semi-random volumes of events and sends by interval")
        |> ignore

        config
            .AddCommand<TimeSeries.CreateBackdatedSeries>("generate")
            .WithAlias("g")
            .WithDescription("Creates backdated volume of entries by company (GUID)")
        |> ignore)

    app.Run(argv)
