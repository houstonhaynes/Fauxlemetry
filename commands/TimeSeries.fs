namespace Commands

module TimeSeries =
    open System
    open System.Threading
    open Spectre.Console.Cli
    open Output

    type EmitSettings() =
        inherit CommandSettings()

        [<CommandOption("-e|--emit")>]
        member val emitInterval = 1000 with get, set

    type GenerateSettings() =
        inherit CommandSettings()

        [<CommandOption("-d|--days")>]
        member val days = 30 with get, set    

    type Emit() =
        inherit Command<EmitSettings>()
        interface ICommandLimiter<EmitSettings>

         override _.Execute(_context, settings) =
            for i in 1..5 do
                let dateTime = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff")
                printMarkedUp $"{ dateTime } :: Data will emit every {emphasize settings.emitInterval} milliseconds" 
                Thread.Sleep(settings.emitInterval)
            0

    type Generate() =
        inherit Command<GenerateSettings>()
        interface ICommandLimiter<GenerateSettings>

         override _.Execute(_context, settings) =
            printMarkedUp $"You've set data to run for {emphasize settings.days} days!"
            0