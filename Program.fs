open Spectre.Console.Cli
open Commands

[<EntryPoint>]
let main argv =

    let app = CommandApp()

    app.Configure(fun config ->
        config.AddCommand<Redis.SpawnData>("spawndata")
            .WithAlias("s")
            .WithDescription("Creates 30 days of test Data for Redis time series.")
            |> ignore)

    app.Run(argv)