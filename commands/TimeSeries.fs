namespace Commands

module TimeSeries =
    open Spectre.Console.Cli
    open Output

    type TimeSeriesSettings() =
        inherit CommandSettings()

        [<CommandOption("-d|--days")>]
        member val days = 30 with get, set
    
    type Generate() =
        inherit Command<TimeSeriesSettings>()
        interface ICommandLimiter<TimeSeriesSettings>

         override _.Execute(_context, settings) =
            printMarkedUp $"You've set data to run for {emphasize settings.days} days!"
            0