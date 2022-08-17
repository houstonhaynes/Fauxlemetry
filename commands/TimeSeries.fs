namespace Commands

open System
open Microsoft.FSharp.Control


module TimeSeries =
    open System.Text.Json
    open System.Text.Json.Serialization
    open System.Threading
    open Microsoft.FSharp.Collections
    open Spectre.Console.Cli
    open Redis.OM
    open Redis.OM.Modeling
    open Output

    // config to generate a number of entries for a given day in the past
    type BackdateSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--vol")>]
        member val volume = 100 with get, set

        [<CommandOption("-r|--rew")>]
        member val rewind: int = 3 with get, set
        
        [<CommandOption("-c|--cust")>]
        member val cst_id = Guid.NewGuid().ToString() with get, set
        
        [<CommandOption("-e|--env")>]
        member val environment = "redis://localhost:6379" with get, set

        [<CommandOption("-f|--flush")>]
        member val flushRedis = false with get, set

        [<CommandOption("-i|--idx")>]
        member val indexRedis = false with get, set

    type EmitSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--vol")>]
        member val volume = 1000 with get, set
        
        [<CommandOption("-c|--cust")>]
        member val cst_id = Guid.NewGuid().ToString() with get, set
        
        [<CommandOption("-e|--env")>]
        member val environment = "redis://localhost:6379" with get, set

    type EventRecord =
        { epoch_timestamp: int64
          EventTime: string
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

    [<Document(StorageType = StorageType.Json, Prefixes = [| "Customer:" |])>]
    type RawDataModel() =

        [<RedisIdField>] 
        member val Id = "" with get, set 

        [<Indexed(Aggregatable = true)>]
        member val epoch_timestamp : int64 = 0 with get, set

        [<Indexed>]
        member val EventTime = "" with get, set

        [<Indexed>]        
        member val cst_id  = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val src_ip = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val src_port = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val dst_ip = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val dst_port = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val cc = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val vpn = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val proxy = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val tor = "" with get, set

        [<Searchable(Aggregatable = true)>]
        member val malware = false with get, set
    
    [<JsonFSharpConverter>]
    type Example = EventRecord

    type CreateBackdatedSeries() =
        inherit Command<BackdateSettings>()
        interface ICommandLimiter<BackdateSettings>
        override _.Execute(_context, settings) =
            
            let mutable currentTime = DateTime.Now.ToString("hh:mm:ss.fff")
            let customer = settings.cst_id

            let provider = RedisConnectionProvider(settings.environment)
            let connection = provider.Connection
            StackExchange.Redis.ConnectionMultiplexer.SetFeatureFlag("preventthreadtheft", true)    

            let RedisCommand = "FLUSHALL"

            let asyncFlushall =
                async {
                    connection.Execute(RedisCommand) |> ignore
                    currentTime <- DateTime.Now.ToString("hh:mm:ss.fff")
                    printMarkedUp $"Sending {warn RedisCommand} to Redis at {info currentTime}"
                }

            if settings.flushRedis = true 
                then    asyncFlushall  |> Async.RunSynchronously
                        currentTime <- DateTime.Now.ToString("hh:mm:ss.fff")
                        printMarkedUp $"Redis {blue RedisCommand} completed at {info currentTime}"


            let RedisCommand = "CREATE INDEX"

            if settings.indexRedis = true then
                currentTime <- DateTime.Now.ToString("hh:mm:ss.fff")
                printMarkedUp $"Sending {warn RedisCommand} to Redis {info currentTime}"
                connection.CreateIndex(typeof<RawDataModel>) |> ignore
                currentTime <- DateTime.Now.ToString("hh:mm:ss.fff")
                printMarkedUp $"Redis {blue RedisCommand} completed at {info currentTime}"
                        
            let createDayForCompany (currentDayOffset : int) =
                async {
                    // set up random functions
                    let rnd = Random()
                    // set the number of days "back in time" for this iteration
                    let dateInPast =
                        DateTime.Now.AddDays(-(currentDayOffset))
                    
                    // create strings for echoing to console and file name  
                    let RewindDate =
                        dateInPast.ToString("yyyy-MM-dd")
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")

                        
                    printMarkedUp $"{info settings.volume} events started for {warn customer} on {emphasize RewindDate} at {info currentCycleTime}"
                        
                    // build an array of randomized time values for hour, minute, second and millis (0 padded)
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
                    
                    // build an array of fake timestamps from the above arrays and sort chronologically (as array of string)
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
                    
                    let epoch_timestamps : int64 array =
                        [| for i in 0 .. (settings.volume-1) -> 
                            DateTimeOffset(DateTime.Parse(randomTimeStamps[i]).ToUniversalTime()).ToUnixTimeMilliseconds()
                        |]

                    // TODO: This should be a lookup of some sort - by country
                    let srcIpFirstOctets = "160.72"
                    // TODO: This should be a lookup of some sort - by company
                    let destIpFistOctets = "10.23"
                    
                    // build an array of randomized octets (3, 4) for the Source and Destination IPv4
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
                
                    // build an array of fake IPv4s from constants and arrays above
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
                                 let randomSrcPort = rnd.Next (1, 101)
                                 match randomSrcPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                                 | _ -> "80" 
                        |]

                    let randomDestPort =
                        [| for i in 0 .. (settings.volume-1)->
                                 let randomDestPort = rnd.Next (1, 101)
                                 match randomDestPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                                 | _ -> "80" 
                        |]
                     
                    // generate array of countries - bias is built from Cloudflare DDoS source country top 10
                    let randomCC =
                        [| for i in 0 .. (settings.volume-1)->
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
                                 | i when i > 38 && i <= 48 -> "IND"
                                 | i when i > 48 && i <= 64 -> "CN"
                                 | _ -> "US"
                        |]
                        
                    // Generate VPN entries for 30% of elements using shuffleR function (and taking top [head] value)
                    let VpnClients =
                        [| for i in 0 .. (settings.volume-1)->
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
                    
                    // generate proxy values - use VpnClients value if present, otherwise create a new value
                    let ProxyClients =
                        [| for i in 0 .. (settings.volume-1)->
                                 let randomProxy = rnd.Next (1, 101)
                                 if randomProxy <= 30 then
                                     if VpnClients[i] <> "" && VpnClients[i] <> "BLANK" then
                                         VpnClients[i]
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
            
                    // Tor values [30%] use VpnClients or ProxyClients value if present, otherwise create new
                    let TorClients =
                        [| for i in 0 .. (settings.volume-1)->
                                 let randomTor = rnd.Next (1, 101)
                                 if randomTor <=30 then
                                     if VpnClients[i] <> "BLANK" ||  ProxyClients[i] <> "BLANK" then
                                         if VpnClients[i] <> "BLANK" then
                                            VpnClients[i]
                                         else
                                            ProxyClients[i]
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

                    // set up an array for MAL booleans - 20% TRUE
                    let MalBoolean =
                        [| for i in 0 .. (settings.volume-1)->
                                 let randomMAL = rnd.Next (1, 101)
                                 match randomMAL with
                                 | i when i = 100 -> "UNKNOWN"
                                 | i when i >= 79 && i <= 99 -> "TRUE"
                                 | _ -> "FALSE"
                        |]
            
                    // create full JSON serializable array
                    let DayRecords =
                        [| for i in 0 .. (settings.volume-1)->
                             { epoch_timestamp = epoch_timestamps[i];
                                 EventTime = randomTimeStamps[i];
                                 cst_id = customer;
                                 src_ip = randomSrcIPv4[i];
                                 src_port = randomSrcPort[i];
                                 dst_ip = randomDestIPv4[i];
                                 dst_port = randomDestPort[i];
                                 cc = randomCC[i];
                                 vpn = VpnClients[i];
                                 proxy = ProxyClients[i];
                                 tor = TorClients[i];
                                 malware = MalBoolean[i]
                                 }
                        |]
                        |> Array.filter (fun record -> record.epoch_timestamp < (DateTimeOffset(DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds()))

                    let serializeRecord (event: EventRecord) = 
                        let newKey = "Customer"+":"+customer+":"+Guid.NewGuid().ToString()
                        connection.Execute("JSON.SET", newKey, "$", JsonSerializer.Serialize(event)) |> ignore
                        let dateTimeNowSeconds = DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()
                        let eventExpirationInSeconds = DateTimeOffset(DateTime.Parse(event.EventTime).AddDays(30)).ToUnixTimeSeconds()
                        let eventTtl = eventExpirationInSeconds - dateTimeNowSeconds
                        connection.Execute("EXPIRE", newKey, eventTtl.ToString())       

                    // serialize JSON
                    let options = JsonSerializerOptions()
                    options.Converters.Add(JsonFSharpConverter())
                    
                    DayRecords
                        |> Array.map serializeRecord
                        |> ignore
            
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                    printMarkedUp $"{warn DayRecords.Length} events generated for {blue customer} on {info RewindDate} at {emphasize currentCycleTime}"
                }
                     
            let daySpan = [| 0 .. (settings.rewind-1) |] 
                            |> Array.rev
            

            let maxThreads = Environment.ProcessorCount
          
            daySpan
                |> Array.map createDayForCompany
                |> fun computations -> Async.Parallel(computations, maxDegreeOfParallelism = maxThreads)
                |> Async.RunSynchronously
                |> ignore


            0

                
    type Emit() =
        inherit Command<EmitSettings>()
        interface ICommandLimiter<EmitSettings>
        override _.Execute(_context, settings) =
            
            // get number of records per minute
            let transmit = true

            let provider = RedisConnectionProvider(settings.environment)
            let connection = provider.Connection

            let customer = settings.cst_id
            let recordsPerMinute : int = Convert.ToInt32(Math.Round(Decimal.Divide(settings.volume, 1440), 0))
            
            // create number of records for that minute

            let createMinuteForCompany =
                async {
                    // set up random functions
                    let rnd = Random()
    
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                        
                    printMarkedUp $"{info recordsPerMinute} events started for {warn customer} at {info currentCycleTime}"
    
                    let systemDateTime = DateTime.Now
    
                    // build an array of randomized time values for hour, minute, second and millis (0 padded)
                    let currentHour = systemDateTime.Hour.ToString()
    
                    let currentMinute = systemDateTime.Minute.ToString()
    
                    let randomSeconds =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(60).ToString().PadLeft(2, '0') 
                        |]                      
    
                    let randomMillis =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(1000).ToString().PadLeft(3, '0') 
                        |]
                    
                    let RewindDate =
                        DateTime.Now.ToString("yyyy-MM-dd")
                    // build an array of fake timestamps from the above arrays and sort chronologically (as array of string)
                    let randomTimeStamps =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 RewindDate
                                 + " "
                                 + currentHour
                                 + ":"
                                 + currentMinute
                                 + ":"
                                 + randomSeconds[i]
                                 + "."
                                 + randomMillis[i] 
                        |]
                    
                    // Prevents the creating of an unnecessary array
                    randomTimeStamps
                    |> Array.sortInPlace
                    
                    let epoch_timestamps : int64 array =
                        [| for i in 0 .. (recordsPerMinute-1) -> 
                            DateTimeOffset(DateTime.Parse(randomTimeStamps[i]).ToUniversalTime()).ToUnixTimeMilliseconds()
                        |]
    
                    // TODO: This should be a lookup of some sort - by country
                    let srcIpFirstOctets = "160.72"
                    // TODO: This should be a lookup of some sort - by company
                    let destIpFistOctets = "10.23"
                    
                    // build an array of randomized octets (3, 4) for the Source and Destination IPv4
                    let randomSrcOctets3 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]
                        
                    let randomSrcOctets4 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]
    
                    let randomDestOctets3 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]
    
                    let randomDestOctets4 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString().PadLeft(3, '0') 
                        |]
                
                    // build an array of fake IPv4s from constants and arrays above
                    let randomSrcIPv4 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 srcIpFirstOctets
                                 + "."
                                 + randomSrcOctets3[i]
                                 + "."
                                 + randomSrcOctets4[i] 
                        |]
    
                    let randomDestIPv4 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 destIpFistOctets
                                 + "."
                                 + randomDestOctets3[i]
                                 + "."
                                 + randomDestOctets4[i]       
                        |]
    
                    let randomSrcPort =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 let randomSrcPort = rnd.Next (1, 101)
                                 match randomSrcPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                                 | _ -> "80" 
                        |]
    
                    let randomDestPort =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 let randomDestPort = rnd.Next (1, 101)
                                 match randomDestPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535).ToString()
                                 | _ -> "80" 
                        |]
                     
                    // generate array of countries - bias is built from Cloudflare DDoS source country top 10
                    let randomCC =
                        [| for i in 0 .. (recordsPerMinute-1)->
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
                                 | i when i > 38 && i <= 48 -> "IND"
                                 | i when i > 48 && i <= 64 -> "CN"
                                 | _ -> "US"
                        |]
                        
                    // Generate VPN entries for 30% of elements using shuffleR function (and taking top [head] value)
                    let VpnClients =
                        [| for i in 0 .. (recordsPerMinute-1)->
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
                    
                    // generate proxy values - use VpnClients value if present, otherwise create a new value
                    let ProxyClients =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 let randomProxy = rnd.Next (1, 101)
                                 if randomProxy <= 30 then
                                     if VpnClients[i] <> "" && VpnClients[i] <> "BLANK" then
                                         VpnClients[i]
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
            
                    // Tor values [30%] use VpnClients or ProxyClients value if present, otherwise create new
                    let TorClients =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 let randomTor = rnd.Next (1, 101)
                                 if randomTor <=30 then
                                     if VpnClients[i] <> "BLANK" ||  ProxyClients[i] <> "BLANK" then
                                         if VpnClients[i] <> "BLANK" then
                                            VpnClients[i]
                                         else
                                            ProxyClients[i]
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
    
                    // set up an array for MAL booleans - 20% TRUE
                    let MalBoolean =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 let randomMAL = rnd.Next (1, 101)
                                 match randomMAL with
                                 | i when i = 100 -> "UNKNOWN"
                                 | i when i >= 79 && i <= 99 -> "TRUE"
                                 | _ -> "FALSE"
                        |]
            
                    // create full JSON serializable array
                    let DayRecords =
                        [| for i in 0 .. (recordsPerMinute-1)->
                             { epoch_timestamp = epoch_timestamps[i];
                                 EventTime = randomTimeStamps[i];
                                 cst_id = customer;
                                 src_ip = randomSrcIPv4[i];
                                 src_port = randomSrcPort[i];
                                 dst_ip = randomDestIPv4[i];
                                 dst_port = randomDestPort[i];
                                 cc = randomCC[i];
                                 vpn = VpnClients[i];
                                 proxy = ProxyClients[i];
                                 tor = TorClients[i];
                                 malware = MalBoolean[i]
                                 }
                        |]
    
                    let serializeRecord (event: EventRecord) = 
                        let newKey = "Customer"+":"+customer+":"+Guid.NewGuid().ToString()
                        connection.Execute("JSON.SET", newKey, "$", JsonSerializer.Serialize(event)) |> ignore
                        let dateTimeNowSeconds = DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()
                        let eventExpirationInSeconds = DateTimeOffset(DateTime.Parse(event.EventTime).AddDays(30)).ToUnixTimeSeconds()
                        let eventTtl = eventExpirationInSeconds - dateTimeNowSeconds
                        connection.Execute("EXPIRE", newKey, eventTtl.ToString())     
    
                    // serialize JSON
                    let options = JsonSerializerOptions()
                    options.Converters.Add(JsonFSharpConverter())
                    
                    DayRecords
                        |> Array.map serializeRecord
                        |> ignore
            
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                    printMarkedUp $"{warn DayRecords.Length} events generated for {blue customer} at {emphasize currentCycleTime}"
                }           

            // transmit records
            let rec emitOnTimer() =
                Thread.Sleep(60000)
                createMinuteForCompany |> Async.Start
    
                if Console.KeyAvailable then 
                    match Console.ReadKey().Key with
                    | ConsoleKey.Q -> ()
                    | _ -> emitOnTimer()
                else                    
                    emitOnTimer()
                
            emitOnTimer()
                           
            0
