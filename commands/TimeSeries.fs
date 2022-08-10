namespace Commands

open System
open FSharp.Json
open Microsoft.FSharp.Control


module TimeSeries =
    open System.IO
    open System.Text.Json
    open System.Text.Json.Serialization
    open System.Threading
    open Microsoft.FSharp.Collections
    open Spectre.Console.Cli
    open Redis.OM
    open Output

    type EmitSettings() =
        inherit CommandSettings()

        [<CommandOption("-g|--gap")>]
        member val emitInterval = 1000 with get, set

    // config to generate a number of entries for a given day in the past
    type BackdateSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--volume")>]
        member val volume = 100 with get, set

        [<CommandOption("-r|--rewind")>]
        member val rewind: int = 3 with get, set
        
        [<CommandOption("-c|--customers")>]
        member val cst_id = Guid.NewGuid().ToString() with get, set
        
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
            
    type EventRecord =
        { EventTime: string
          cst_id: string
          src_ip: string
          src_port: string
          dst_ip: string
          dst_port: string
          cc: string
          vpn: string
          proxy: string
          tor: string
          malware: string }
    
    [<JsonFSharpConverter>]
    type Example = EventRecord

    type CreateBackdatedSeries() =
        inherit Command<BackdateSettings>()
        interface ICommandLimiter<BackdateSettings>
        override _.Execute(_context, settings) =
            
            // bring in customer GUID
            let customer : String = settings.cst_id
            let directoryPath = "datagen/"+customer+"/"
            // create directory in run path if it doesn't exist.
            Directory.CreateDirectory(directoryPath) |> ignore 

            // get directory info - for files - if they exist
            let dir = DirectoryInfo(directoryPath)
            // extract file directory info into a string list
            let files = 
                dir.EnumerateFiles()
                |> Array.ofSeq
                |> Array.map (fun file -> file.FullName)
                
            // clear the folder of files by iterating over the list  (no harm if empty)
            Array.iter (File.Delete) files
            
            // show current time
            let currentFileTime = DateTime.Now.ToString("hh:mm:ss.fff")
            printMarkedUp $"Current time is {emphasize currentFileTime} !"
               
            let provider = RedisConnectionProvider("redis://localhost:6379")
            let connection = provider.Connection
                        
            let createDayForCompany (currentDayOffset : int) =
                async {
                    // set up random functions
                    let rnd = Random()
    //                let shuffleR (r : Random) xs = xs |> Array.sortBy (fun _ -> r.Next())
                    // set the number of days "back in time" for this iteration
                    let dateInPast =
                        DateTime.Now.AddDays(-(currentDayOffset))
                    
                    // create strings for echoing to console and file name  
                    let RewindDate =
                        dateInPast.ToString("yyyy-MM-dd")
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                    let fileName = RewindDate+"_"+customer+".json"
                    let filePath = (directoryPath+fileName)
                    
                    // TODO: set Progress indicator for 0-1%
                        
                    printMarkedUp $"{info settings.volume} events started for {warn customer} on {emphasize RewindDate} at {info currentCycleTime} !"
                        
                    // build a list of randomized time values for hour, minute, second and millis (0 padded)
                    let randomHours = 
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(24).ToString().PadLeft(2, '0') 
                        |]

                    let randomMinutes =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(60).ToString().PadLeft(2, '0') 
                        |]

                    let randomSeconds =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(60).ToString().PadLeft(2, '0') 
                        |]                      

                    let randomMillis =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(1000).ToString().PadLeft(3, '0') 
                        |]
                    
                    // build a list of fake timestamps from the above lists and sort chronologically (as list of string)
                    let randomTimeStamps =
                        [| for i in 0 .. (settings.volume-1)->
                                 RewindDate
                                 + " "
                                 + randomHours[i]
                                 + ":"
                                 + randomMinutes[i]
                                 + ":"
                                 + randomSeconds[i]
                                 + "."
                                 + randomMillis[i] 
                        |]
                    
                    // Prevents the creating of an unnecessary array
                    randomTimeStamps
                    |> Array.sortInPlace
                        
                    // TODO: set Progress indicator for 10%    

                    // TODO: This should be a lookup of some sort - by country
                    let srcIpFirstOctets = "160.72"
                    // TODO: This should be a lookup of some sort - by company
                    let destIpFistOctets = "10.23"
                    
                    // build a list of randomized octets (3, 4) for the Source and Destination IPv4
                    let randomSrcOctets3 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]
                        
                    let randomSrcOctets4 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]

                    let randomDestOctets3 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]

                    let randomDestOctets4 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]
                
                    // build a list of fake IPv4s from constants and lists above
                    let randomSrcIPv4 =
                        [| for i in 0 .. (settings.volume-1)->
                                 srcIpFirstOctets
                                 + "."
                                 + randomSrcOctets3[i]
                                 + "."
                                 + randomSrcOctets4[i] 
                        |]

                    let randomDestIPv4 =
                        [| for i in 0 .. (settings.volume-1)->
                                 destIpFistOctets
                                 + "."
                                 + randomDestOctets3[i]
                                 + "."
                                 + randomDestOctets4[i]       
                        |]

                    let randomSrcPort =
                        [| for i in 0 .. (settings.volume-1)->
    //                             let randomSrcPort = [|1..100|] |> shuffleR (Random()) |> Array.head
                                 let randomSrcPort = rnd.Next (1, 101)
                                 match randomSrcPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                                 | _ -> "80" 
                        |]

                    let randomDestPort =
                        [| for i in 0 .. (settings.volume-1)->
    //                             let randomDestPort = [|1..100|] |> shuffleR (Random()) |> Array.head
                                 let randomDestPort = rnd.Next (1, 101)
                                 match randomDestPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                                 | _ -> "80" 
                        |]

                    // TODO: set Progress indicator for 30%   

                    // generate list of countries - bias is built from Cloudflare DDoS source country top 10
                    let randomCC =
                        [| for i in 0 .. (settings.volume-1)->
    //                             let randomCountry = [|1..100|] |> shuffleR (Random()) |> Array.head
                                 let randomCountry = rnd.Next (1, 101)
                                 match randomCountry with
                                 | i when i < 10 -> "UNKNOWN"
                                 | i when i > 10 && i <= 14 -> "RU"
                                 | i when i > 14 && i <= 18 -> "ID"
                                 | i when i > 18 && i <= 22 -> "EG"
                                 | i when i > 22 && i <= 26 -> "KR"
                                 | i when i > 26 && i <= 30 -> "UA"
                                 | i when i > 30 && i <= 34 -> "BR"
                                 | i when i > 34 && i <= 38 -> "DE"
                                 | i when i > 38 && i <= 48 -> "IN"
                                 | i when i > 48 && i <= 64 -> "CN"
                                 | _ -> "US"
                        |]
                        
                    // TODO: set Progress indicator for 35% 
                        
                    // Generate VPN entries for 30% of elements using shuffleR function (and taking top [head] value)
                    let VpnClientList =
                        [| for i in 0 .. (settings.volume-1)->
    //                             let randomVPN = [|1..100|] |> shuffleR (Random()) |> Array.head
                                 let randomVPN = rnd.Next (1, 101)
                                 match randomVPN with
                                 | i when i > 1 && i <= 5 -> "nord;proton"
                                 | i when i > 5 && i <= 10 -> "nord;surfshark"
                                 | i when i > 10 && i <= 15 -> "nord;foxyproxy"
                                 | i when i > 15 && i <= 18 -> "purevpn"
                                 | i when i > 18 && i <= 21 -> "proton"
                                 | i when i > 21 && i <= 24 -> "nord"
                                 | i when i > 24 && i <= 27 -> "foxyproxy"
                                 | i when i > 27 && i <= 30 -> "surfshark"
                                 | _ -> "BLANK"
                        |]
                        
                    // TODO: set Progress indicator for 40%     
                    
                    // generate proxy values - use VpnClientList value if present, otherwise create a new value
                    let ProxyClientList =
                        [| for i in 0 .. (settings.volume-1)->
    //                             let randomProxy = [|1..100|] |> shuffleR (Random()) |> Array.head
                                 let randomProxy = rnd.Next (1, 101)
                                 if randomProxy <= 30 then
                                     if VpnClientList[i] <> "" && VpnClientList[i] <> "BLANK" then
                                         VpnClientList[i]
                                     else
                                         match randomProxy with
                                         | i when i > 1 && i <= 5 -> "nord;proton"
                                         | i when i > 5 && i <= 10 -> "nord;surfshark"
                                         | i when i > 10 && i <= 15 -> "nord;foxyproxy"
                                         | i when i > 15 && i <= 18 -> "purevpn"
                                         | i when i > 18 && i <= 21 -> "proton"
                                         | i when i > 21 && i <= 24 -> "nord"
                                         | i when i > 24 && i <= 27 -> "foxyproxy"
                                         | i when i > 27 && i <= 30 -> "surfshark"
                                         | _ -> "BLANK"
                                 else
                                     "BLANK"
                        |]
                        
                    // TODO: set Progress indicator for 50% 
            
                    // Tor values [30%] use VpnClientList or ProxyClientList value if present, otherwise create new
                    let TorClientList =
                        [| for i in 0 .. (settings.volume-1)->
    //                             let randomTor = [|1..100|] |> shuffleR (Random()) |> Array.head
                                 let randomTor = rnd.Next (1, 101)
                                 if randomTor <=30 then
                                     if VpnClientList[i] <> "BLANK" ||  ProxyClientList[i] <> "BLANK" then
                                         if VpnClientList[i] <> "BLANK" then
                                            VpnClientList[i]
                                         else
                                            ProxyClientList[i]
                                     else
                                         match randomTor with
                                         | i when i > 1 && i <= 5 -> "nord;proton"
                                         | i when i > 5 && i <= 10 -> "nord;surfshark"
                                         | i when i > 10 && i <= 15 -> "nord;foxyproxy"
                                         | i when i > 15 && i <= 18 -> "purevpn"
                                         | i when i > 18 && i <= 21 -> "proton"
                                         | i when i > 21 && i <= 24 -> "nord"
                                         | i when i > 24 && i <= 27 -> "foxyproxy"
                                         | i when i > 27 && i <= 30 -> "surfshark"
                                         | _ -> "BLANK"
                                 else
                                     "BLANK"
                        |]
                        
                    // TODO: set Progress indicator for 65%     

                    // set up a list for MAL booleans - 20% TRUE
                    let MalBoolean =
                        [| for i in 0 .. (settings.volume-1)->
    //                             let randomMAL = [|1..100|] |> shuffleR (Random()) |> Array.head
                                 let randomMAL = rnd.Next (1, 101)
                                 match randomMAL with
                                 | i when i = 100 -> "UNKNOWN"
                                 | i when i >= 79 && i <= 99 -> "TRUE"
                                 | _ -> "FALSE"
                        |]

                        
                    // TODO: set Progress indicator for 75%     
            
                    // create full JSON serializable list
                    let DayRecordList =
                        [| for i in 0 .. (settings.volume-1)->
                             { EventTime = randomTimeStamps[i];
                                 cst_id = customer;
                                 src_ip = randomSrcIPv4[i];
                                 src_port = randomSrcPort[i];
                                 dst_ip = randomDestIPv4[i];
                                 dst_port = randomDestPort[i];
                                 cc = randomCC[i];
                                 vpn = VpnClientList[i];
                                 proxy = ProxyClientList[i];
                                 tor = TorClientList[i];
                                 malware = MalBoolean[i]
                                 }|]
                                 
                                   
                    let serializeRecord event =
                        connection.Execute("JSON.SET", Guid.NewGuid().ToString(), "$", JsonSerializer.Serialize(event)) |> ignore
                    
                    // TODO: set Progress indicator for 90% 

                    // serialize JSON
                    let options = JsonSerializerOptions()
                    options.Converters.Add(JsonFSharpConverter())
            
                    // let DayRecordJSON =
                    //     JsonSerializer.Serialize (DayRecordList, options)
                    //
                    // // write the file
                    // File.AppendAllText(filePath, DayRecordJSON) |> ignore
                    
                    DayRecordList
                        |> Array.iter serializeRecord

                   
                    // TODO: set Progress indicator for 100% 
            
                    //printfn "%A" DayRecordJSON
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                    printMarkedUp $"{warn settings.volume} events generated for {blue customer} on {info RewindDate} at {emphasize currentCycleTime} !"
                }
                     
            let daySpan = [|0 .. (settings.rewind-1)|] // I like the zero indexing to get a day's worth for *current* day
            
            daySpan
                |> Array.map createDayForCompany
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
                //       
            0
