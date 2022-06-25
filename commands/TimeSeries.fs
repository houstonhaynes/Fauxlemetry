namespace Commands

module TimeSeries =
    open Spectre.Console.Cli
    open Output

    type SpawnDataSettings() =
        inherit CommandSettings()

        [<CommandOption("-d|--days")>]
        member val days = 30 with get, set
    
    type SpawnData() =
        inherit Command<SpawnDataSettings>()
        interface ICommandLimiter<SpawnDataSettings>

         override _.Execute(_context, settings) =
            printMarkedUp $"You've set data to run for {emphasize settings.days} days!"
            0