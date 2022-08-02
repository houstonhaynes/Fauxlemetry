namespace Commands

open System
open System.Web
open Microsoft.FSharp.Control
open Spectre.Console


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
                |> Seq.map (fun file -> file.FullName)
                |> List.ofSeq
            // clear the folder of files by iterating over the list  (no harm if empty)
            List.iter (File.Delete) files
            
            // show current time
            let currentFileTime = DateTime.Now.ToString("hh:mm:ss.fff")
            printMarkedUp $"Current time is {emphasize currentFileTime} !"
                        
            let createDayForCompany (currentDayOffset : int) = async {
                    // set up random functions
                    let rnd = Random()
                    let shuffleR (r : Random) xs = xs |> Seq.sortBy (fun _ -> r.Next())
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
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(24).ToString().PadLeft(2, '0') 
                        }
                        |> Seq.toList
    
                    let randomMinutes =
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(60).ToString().PadLeft(2, '0') 
                        }
                        |> Seq.toList
    
                    let randomSeconds =
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(60).ToString().PadLeft(2, '0') 
                        }
                        |> Seq.toList                        
    
                    let randomMillis =
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(1000).ToString().PadLeft(3, '0') 
                        }
                        |> Seq.toList
                    
                    // build a list of fake timestamps from the above lists and sort chronologically (as list of string)
                    let randomTimeStamps =
                        seq { for i in 0 .. settings.volume do
                              RewindDate
                              + " "
                              + randomHours[i]
                              + ":"
                              + randomMinutes[i]
                              + ":"
                              + randomSeconds[i]
                              + "."
                              + randomMillis[i] 
                        }
                        |> Seq.toList
                        |> List.sort
                        
                    // TODO: set Progress indicator for 10%    
    
                    // TODO: This should be a lookup of some sort - by country
                    let srcIpFirstOctets = "160.72"
                    // TODO: This should be a lookup of some sort - by company
                    let destIpFistOctets = "10.23"
                    
                    // build a list of randomized octets (3, 4) for the Source and Destination IPv4
                    let randomSrcOctets3 =
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(256).ToString().PadLeft(3, '0') 
                        }
                        |> Seq.toList
                        
                    let randomSrcOctets4 =
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(256).ToString().PadLeft(3, '0') 
                        }
                        |> Seq.toList
    
                    let randomDestOctets3 =
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(256).ToString().PadLeft(3, '0') 
                        }
                        |> Seq.toList
    
                    let randomDestOctets4 =
                        seq { for i in 0 .. settings.volume do
                              rnd.Next(256).ToString().PadLeft(3, '0') 
                        }
                        |> Seq.toList
                
                    // build a list of fake IPv4s from constants and lists above
                    let randomSrcIPv4 =
                        seq { for i in 0 .. settings.volume do
                              srcIpFirstOctets
                              + "."
                              + randomSrcOctets3[i]
                              + "."
                              + randomSrcOctets4[i] 
                        }
                        |> Seq.toList
    
                    let randomDestIPv4 =
                        seq { for i in 0 .. settings.volume do
                              destIpFistOctets
                              + "."
                              + randomDestOctets3[i]
                              + "."
                              + randomDestOctets4[i]       
                        }
                        |> Seq.toList
    
                    let randomSrcPort =
                        seq { for i in 0 .. settings.volume do
                              let randomSrcPort = [1..100] |> shuffleR (Random()) |> Seq.head
                              match randomSrcPort with
                              | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                              | _ -> "80" 
                        }
                        |> Seq.toList
    
                    let randomDestPort =
                        seq { for i in 0 .. settings.volume do
                              let randomDestPort = [1..100] |> shuffleR (Random()) |> Seq.head
                              match randomDestPort with
                              | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                              | _ -> "80" 
                        }
                        |> Seq.toList
    
                        
                    // TODO: set Progress indicator for 30%   
    
                    // generate list of countries - bias is built from Cloudflare DDoS source country top 10
                    let randomCC =
                        seq { for i in 0 .. settings.volume do
                              let randomCountry = [1..100] |> shuffleR (Random()) |> Seq.head
                              match randomCountry with
                              | i when i < 10 -> "UNKNOWN"
                              | i when i > 10 && i <= 14 -> "RU"
                              | i when i > 14 && i <= 18-> "ID"
                              | i when i > 18 && i <= 22  -> "EG"
                              | i when i > 22 && i <= 26 -> "SG"
                              | i when i > 26 && i <= 30 -> "UA"
                              | i when i > 30 && i <= 34 -> "BR"
                              | i when i > 34 && i <= 38 -> "DE"
                              | i when i > 38 && i <= 48 -> "IN"
                              | i when i > 48 && i <= 64 -> "CN"
                              | _ -> "US"
                        }
                        |> Seq.toList
                        
                    // TODO: set Progress indicator for 35% 
                        
                    // Generate VPN entries for 30% of elements using shuffleR function (and taking top [head] value)
                    let VpnClientList =
                        seq { for i in 0 .. settings.volume do
                              let randomVPN = [1..100] |> shuffleR (Random()) |> Seq.head
                              match randomVPN with
                              | i when i > 1 && i <= 5 -> "nord;proton"
                              | i when i > 5 && i <= 10 -> "nord;surfshark"
                              | i when i > 10 && i <= 15 -> "nord;foxyproxy"
                              | i when i > 15 && i <= 18 -> "purevpn"
                              | i when i > 18 && i <= 21 -> "proton"
                              | i when i > 21 && i <= 24 -> "nord"
                              | i when i > 24 && i <= 27 -> "foxyproxy"
                              | i when i > 27 && i <= 30 -> "surfshark"
                              | _ -> ""
                        }
                        |> Seq.toList
                        
                    // TODO: set Progress indicator for 40%     
                    
    
                    // generate proxy values - use VpnClientList value if present, otherwise create a new value
                    let ProxyClientList =
                        seq { for i in 0 .. settings.volume do
                              let randomProxy = [1..100] |> shuffleR (Random()) |> Seq.head
                              if randomProxy <= 30 then
                                  if VpnClientList[i] <> "" then
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
                                        | _ -> ""
                              else
                                  ""
                            }
                        |> Seq.toList
                        
                    // TODO: set Progress indicator for 50% 
            
                    // Tor values [30%] use VpnClientList or ProxyClientList value if present, otherwise create new
                    let TorClientList =
                        seq { for i in 0 .. settings.volume do
                              let randomTor = [1..100] |> shuffleR (Random()) |> Seq.head
                              if randomTor <=30 then
                                  if (VpnClientList[i] <> "" || ProxyClientList[i] <> "") then
                                      if VpnClientList[i] <> "" then
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
                                        | _ -> ""
                              else
                                  ""
                        }
                        |>Seq.toList
                        
                    // TODO: set Progress indicator for 65%     
    
                    // set up a list for MAL booleans - 20% TRUE
                    let MalBoolean =
                        seq { for i in 0 .. settings.volume do
                              let randomMAL = [1..100] |> shuffleR (Random()) |> Seq.head
                              match randomMAL with
                              | i when i = 100 -> "UNKNOWN"
                              | i when i >= 79 && i <= 99 -> "TRUE"
                              | _ -> "FALSE"
                        }
                        |> Seq.toList
                        
                    // TODO: set Progress indicator for 75%     
            
                    // create full JSON serializable list
                    let DayRecordList =
                        [ for i in 0 .. settings.volume do
                            { EventTime = randomTimeStamps[i]
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
                              }]
                        
                    // TODO: set Progress indicator for 90% 
    
                    // serialize JSON
                    let options = JsonSerializerOptions()
                    options.Converters.Add(JsonFSharpConverter())
            
                    let DayRecordJSON =
                        JsonSerializer.Serialize (DayRecordList, options)
                    
                    // write the file
                    File.AppendAllText(filePath, DayRecordJSON) |> ignore

                    // cleanup if .DS_Store exists
                    if File.Exists(filePath+".DS_Store") then
                        File.Delete(filePath+".DS_Store")
                    
                    // TODO: set Progress indicator for 100% 
            
                    //printfn "%A" DayRecordJSON
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                    printMarkedUp $"{warn settings.volume} events generated for {blue customer} on {info RewindDate} at {emphasize currentCycleTime} !"
            }
            
            
//            let dayLabels = 
//                [ for i = settings.rewind downto 0 do
//                    DateTime.Now.AddDays(-(daySpan[i])).ToString("yyyy-mm-dd")
//                ]
//            
//            // instantiate Progress - is this even right?    
//            let fauxProgress =
//                AnsiConsole.Progress()
//            
//            fauxProgress.Start(fun ->  // this is pseudo-code I really don't know what I'm doing here
//                        {
//                          [for i in 0 .. dayLabels.Length do
//                            ctx.AddTask("[green]"+{dayLabels[i]}+"[/]")]
//                        }) 
            
            let daySpan = [0 .. settings.rewind]
            
            daySpan
                |> List.map createDayForCompany
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
                      
            0
