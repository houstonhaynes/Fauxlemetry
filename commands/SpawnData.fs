namespace Commands

module SpawnData =
    open Spectre.Console.Cli
    open Output

    type SpawnDataSettings() =
        inherit CommandSettings()

        [<CommandOption("-n|--name")>]
        member val name = "friend" with get, set
    
    type SpawnData() =
        inherit Command<SpawnDataSettings>()
        interface ICommandLimiter<SpawnDataSettings>

        override _.Execute(_context, settings) = 
            printMarkedUp $"Hello {emphasize settings.name}! This command spawns data from F#."
            0