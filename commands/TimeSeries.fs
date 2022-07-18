namespace Commands

open System
open System.Web


module TimeSeries =
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Serialization
    open System.Threading
    open Microsoft.FSharp.Collections
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

    // config to generate a number of entries for a given day in the past
    type BackdateSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--volume")>]
        member val volume = 10000 with get, set

        [<CommandOption("-r|--rewind")>]
        member val rewind: int = 30 with get, set
        
        [<CommandOption("-c|--customers")>]
        member val cst_id = Guid.NewGuid() with get, set

    type Emit() =
        inherit Command<EmitSettings>()
        interface ICommandLimiter<EmitSettings>

        override _.Execute(_context, settings) =
            for i in 1..5 do
                let dateTime =
                    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff")

                printMarkedUp $"{dateTime} :: Data will emit every {emphasize settings.emitInterval} milliseconds"
                Thread.Sleep(settings.emitInterval)

            0

    type Generate() =
        inherit Command<GenerateSettings>()
        interface ICommandLimiter<GenerateSettings>

        override _.Execute(_context, settings) =
            printMarkedUp $"You've set data to run for {emphasize settings.days} days!"
            0

    type EventRecord =
        { EventTime: string
          cst_id: Guid
          src_ip: string
          src_port: int
          dst_ip: string
          dst_port: int
          cc: string
          vpn: string
          proxy: string
          tor: string
          malware: Boolean }

    type CreateBackdatedSeries() =
        inherit Command<BackdateSettings>()
        interface ICommandLimiter<BackdateSettings>
        override _.Execute(_context, settings) =
            // create file to append all records
            let mutable rewind = settings.rewind
            let dateInPast =
                DateTime.Now.AddDays(-(settings.rewind))
            let backDate =
                dateInPast.ToString("yyyy_MM_dd")
            let fileDateTime =
                DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss")
            let fileName = backDate+"_EventData_"+fileDateTime+".json"
            
            // set up random functions
            let rnd = Random()
            let shuffleR (r : Random) xs = xs |> Seq.sortBy (fun _ -> r.Next())
            
            // show current time
            let currentFileTime = DateTime.Now.ToString("hh:mm:ss.fff")
            printMarkedUp $"Current time is {emphasize currentFileTime} !"
            
            while rewind >= 0 do
                let dateInPast =
                    DateTime.Now.AddDays(-(rewind))
                let RewindDate =
                    dateInPast.ToString("MM/dd/yyyy")
                // build a list of randomized time values for hour, minute, second and millis (0 padded)
                let randomHours =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(24).ToString().PadLeft(2, '0') ]

                let randomMinutes =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(60).ToString().PadLeft(2, '0') ]

                let randomSeconds =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(60).ToString().PadLeft(2, '0') ]

                let randomMillis =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(1000).ToString().PadLeft(3, '0') ]
                // build a list of fake timestamps from the above lists and sort chronologically
                let randomTimeStamps =
                    [ for i in 0 .. settings.volume do
                          RewindDate
                          + " "
                          + randomHours[i]
                          + ":"
                          + randomMinutes[i]
                          + ":"
                          + randomSeconds[i]
                          + "."
                          + randomMillis[i] ]
                    |> List.sort

                // TODO: create dictionaries with lists for IPv4 first octets per country
                let srcIpFirstOctets = "160.72"
                // TODO: create dictionaries with lists for IPv4 first octets per company
                let destIpFistOctets = "10.23"
                // build a list of randomized octets (3, 4) for the Source and Destination IPv4
                let randomSrcOctets3 =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(256).ToString().PadLeft(3, '0') ]
                let randomSrcOctets4 =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(256).ToString().PadLeft(3, '0') ]

                let randomDestOctets3 =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(256).ToString().PadLeft(3, '0') ]

                let randomDestOctets4 =
                    [ for i in 0 .. settings.volume do
                          rnd.Next(256).ToString().PadLeft(3, '0') ]
                    
                // build a list of fake IPv4s from constants and lists above
                let randomSrcIPv4 =
                    [ for i in 0 .. settings.volume do
                          srcIpFirstOctets
                          + "."
                          + randomSrcOctets3[i]
                          + "."
                          + randomSrcOctets4[i] ]

                let randomDestIPv4 =
                    [ for i in 0 .. settings.volume do
                          destIpFistOctets
                          + "."
                          + randomDestOctets3[i]
                          + "."
                          + randomDestOctets4[i] ]

                let randomSrcPort =
                    [ for i in 0 .. settings.volume do
                          let randomSrcPort = [1..100] |> shuffleR (Random()) |> Seq.head
                          match randomSrcPort with
                          | i when i > 90 -> rnd.Next(1025, 65535)
                          | _ -> 80]

                let randomDestPort =
                    [ for i in 0 .. settings.volume do
                          let randomDestPort = [1..100] |> shuffleR (Random()) |> Seq.head
                          match randomDestPort with
                          | i when i > 90 -> rnd.Next(1025, 65535)
                          | _ -> 80]


                // generate list of countries
                let randomCC =
                    [ for i in 0 .. settings.volume do
                          let randomCountry = [1..100] |> shuffleR (Random()) |> Seq.head
                          match randomCountry with
                          | i when i < 5 -> "RU"
                          | i when i > 5 && i <= 10-> "ID"
                          | i when i > 10 && i <= 15  -> "EG"
                          | i when i > 15 && i <= 20 -> "SG"
                          | i when i > 20 && i <= 25 -> "UA"
                          | i when i > 25 && i <= 30 -> "BR"
                          | i when i > 30 && i <= 35 -> "DE"
                          | i when i > 35 && i <= 45 -> "IN"
                          | i when i > 45 && i <= 60 -> "CN"
                          | _ -> "US"
                    ]

                // select 30% of values to populate in rows
                let VpnClientList =
                    [ for i in 0 .. settings.volume do
                          let randomVPN = [1..100] |> shuffleR (Random()) |> Seq.head
                          match randomVPN with
                          | i when i > 1 && i <= 3 -> "nord;proton"
                          | i when i > 3 && i <= 5 -> "nord;surfshark"
                          | i when i > 5 && i <= 7 -> "nord;foxyproxy"
                          | i when i > 7 && i <= 11 -> "purevpn"
                          | i when i > 11 && i <= 15 -> "proton"
                          | i when i > 15 && i <= 20 -> "nord"
                          | i when i > 20 && i <= 25 -> "foxyproxy"
                          | i when i > 25 && i <= 30 -> "surfshark"
                          | _ -> ""
                    ]
                          

                // select top 20%ish of values - use VpnClientList value if present, otherwise get a new value
                let ProxyClientList =
                    [ for i in 0 .. settings.volume do
                      let randomProxy = [1..100] |> shuffleR (Random()) |> Seq.head
                      if randomProxy <= 30 then
                          if VpnClientList[i] <> "" then
                              VpnClientList[i]
                          else
                              match randomProxy with
                              | i when i > 1 && i <= 3 -> "nord;proton"
                              | i when i > 3 && i <= 5 -> "nord;surfshark"
                              | i when i > 5 && i <= 7 -> "nord;foxyproxy"
                              | i when i > 7 && i <= 11 -> "purevpn"
                              | i when i > 11 && i <= 15 -> "proton"
                              | i when i > 15 && i <= 20 -> "nord"
                              | i when i > 20 && i <= 25 -> "foxyproxy"
                              | i when i > 25 && i <= 30 -> "surfshark"
                              | _ -> ""
                      else
                          ""
                    ]
                
                // select top 20%ish of values - use VpnClientList or ProxyClientList value, otherwise get new
                let TorClientList =
                    [ for i in 0 .. settings.volume do
                          let randomTor = [1..100] |> shuffleR (Random()) |> Seq.head
                          if randomTor <=30 then
                              if (VpnClientList[i] <> "" || ProxyClientList[i] <> "") then
                                  if VpnClientList[i] <> "" then
                                      VpnClientList[i]
                                  else
                                      ProxyClientList[i]
                              else
                                  match randomTor with
                                  | i when i > 1 && i <= 3 -> "nord;proton"
                                  | i when i > 3 && i <= 5 -> "nord;surfshark"
                                  | i when i > 5 && i <= 7 -> "nord;foxyproxy"
                                  | i when i > 7 && i <= 11 -> "purevpn"
                                  | i when i > 11 && i <= 15 -> "proton"
                                  | i when i > 15 && i <= 20 -> "nord"
                                  | i when i > 20 && i <= 25 -> "foxyproxy"
                                  | i when i > 25 && i <= 30 -> "surfshark"
                                  | _ -> ""
                          else
                              ""
                    ]


                // set up a list for MAL booleans
                let MalBoolean =
                    [ for i in 0 .. settings.volume do
                          let randomMAL = [1..100] |> shuffleR (Random()) |> Seq.head
                          match randomMAL with
                          | i when i > 80 -> true
                          | _ -> false]
                
                // create full JSON serializable list
                let DayRecordList : EventRecord List =
                    [ for i in 0 .. settings.volume do
                        { EventTime = randomTimeStamps[i]
                          cst_id = settings.cst_id;
                          src_ip = randomSrcIPv4[i];
                          src_port = randomSrcPort[i];
                          dst_ip = randomDestIPv4[i];
                          dst_port = randomDestPort[i];
                          cc = randomCC[i];
                          vpn = VpnClientList[i];
                          proxy = ProxyClientList[i];
                          tor = TorClientList[i];
                          malware = MalBoolean[i]
                          }]

                // serialize JSON
                let options = JsonSerializerOptions()
                options.Converters.Add(JsonFSharpConverter())
                
                let DayRecordJSON =
                    JsonSerializer.Serialize (DayRecordList, options)

                // write the file
                File.AppendAllTextAsync(fileName, DayRecordJSON)
                
                //printfn "%A" DayRecordJSON
                rewind <- rewind - 1
                let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                printMarkedUp $"{info settings.volume} events generated for {emphasize RewindDate} at {emphasize currentCycleTime} !"

            0
