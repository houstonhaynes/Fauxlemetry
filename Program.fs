open Spectre.Console.Cli
open Commands

[<EntryPoint>]
let main argv =

    let app = CommandApp()

    app.Configure (fun config ->
        config
            .AddCommand<TimeSeries.Generate>("days")
            .WithAlias("d")
            .WithDescription("Creates 30 days of test Data for Redis time series.")
        |> ignore

        config
            .AddCommand<TimeSeries.Emit>("emit")
            .WithAlias("e")
            .WithDescription("Creates a value with an emit interval constant")
        |> ignore

        config
            .AddCommand<TimeSeries.CreateBackdatedSeries>("backdate")
            .WithAlias("b")
            .WithDescription("Creates backdated series of entries")
        |> ignore)

    app.Run(argv)
