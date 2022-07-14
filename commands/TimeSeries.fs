namespace Commands

open MathNet.Numerics
open FSharp.Data

module TimeSeries =
    open System
    open System.Threading
    open Spectre.Console.Cli
    open Output

    type EmitSettings() =
        inherit CommandSettings()

        [<CommandOption("-g|--gap")>]
        member val emitInterval = 1000 with get, set

    type GenerateSettings() =
        inherit CommandSettings()

        [<CommandOption("-w|--window")>]
        member val days = 30 with get, set
    
    // generate a number of entries for a given day in the past    
    type BackdateSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--volume")>]
        member val volume = 100 with get, set
        
        [<CommandOption("-r|--rewind")>]
        member val rewind : int = 1 with get, set    

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
            
    type CreateBackdatedSeries() =
        inherit Command<BackdateSettings>()
        interface ICommandLimiter<BackdateSettings>

        override _.Execute(_context, settings) =
            let dateInPast = DateTime.Now.AddDays(-(settings.rewind))
            let RewindDate = dateInPast.ToString("MM/dd/yyyy")
            
            // set up random functions
            let rnd = Random()
            let shuffleR (r : Random) xs = xs |> Seq.sortBy (fun _ -> r.Next())
            
            let CountryList = ["RU";"RU";"RU";"RU";"RU";"RU";"RU";"RU";"RU";"RU";"RU";"RU"
                               "CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN"
                               "CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN";"CN"
                               "UK";"UK";"UK";"UK";"US";"US";"US";"US";"US";"US";"IT";"CA";"MX"]
            
            let randomCC = [for i in 0..settings.volume do CountryList.[rnd.Next(CountryList.Length)]]
            
            // set up a list to configure bias for booleans
                        
            let randomMAL = MathNet.Numerics.Combinatorics.GeneratePermutation(100)
            let MalBoolean = [for i in 0..settings.volume do
                                  if randomMAL[i] > 80 then
                                      "TRUE"
                                  else
                                      "FALSE"]
            
            let randomVPN = MathNet.Numerics.Combinatorics.GeneratePermutation(100)
            
            
            
            // build a list of randomized time values
            let randomHours = [for i in 0..settings.volume do rnd.Next(23).ToString().PadLeft(2, '0')]
            let randomMinutes = [for i in 0..settings.volume do rnd.Next(59).ToString().PadLeft(2, '0')]
            let randomSeconds = [for i in 0..settings.volume do rnd.Next(59).ToString().PadLeft(2, '0')]
            let randomMillis = [for i in 0..settings.volume do rnd.Next(999).ToString().PadLeft(3, '0')]
            
            // build a list of fake timestamps
            let randomTimeStamps = [for i in 0..settings.volume do
                                        RewindDate+" "+randomHours[i]+":"+randomMinutes[i]+":"+randomSeconds[i]+"."+randomMillis[i]] |> List.sort
            
            let srcIpFirstOctets = "160.72"
            let destIpFistOctets = "10.23"
            
            // build a list of randomized octets for the Source and Destination IPv4
            let randomSrcOctets3 = [for i in 0..settings.volume do rnd.Next(256).ToString().PadLeft(3, '0')]
            let randomSrcOctets4 = [for i in 0..settings.volume do rnd.Next(256).ToString().PadLeft(3, '0')]
            let randomDestOctets3 = [for i in 0..settings.volume do rnd.Next(256).ToString().PadLeft(3, '0')]
            let randomDestOctets4 = [for i in 0..settings.volume do rnd.Next(256).ToString().PadLeft(3, '0')]
            
            // build a list of fake IPv4s
            let randomSrcIPv4 = [for i in 0..settings.volume do
                                        srcIpFirstOctets+"."+randomSrcOctets3[i]+"."+randomSrcOctets4[i]]
            let randomDestIPv4 = [for i in 0..settings.volume do
                                        destIpFistOctets+"."+randomDestOctets3[i]+"."+randomDestOctets4[i]]
            
            // build a list of randomized ports for the Dest
            let randomSrcPort = [for i in 0..settings.volume do rnd.Next(80, 8000).ToString()]
            let randomDestPort= [for i in 0..settings.volume do rnd.Next(80, 8000).ToString()]
            
            printf "%A" randomTimeStamps;printfn""
            printf "%A" randomSrcIPv4;printfn""
            printf "%A" randomSrcPort;printfn""
            printf "%A" randomDestIPv4;printfn""
            printf "%A" randomDestPort;printfn""
            printf "%A" randomCC;printfn""
            printf "%A" randomMAL;printfn""
            printf "%A" MalBoolean;printfn""

            printMarkedUp $"You've set rewind for {emphasize RewindDate} and to generate {info settings.volume} events!"
            0