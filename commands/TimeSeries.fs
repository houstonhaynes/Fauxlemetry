namespace Commands

open System
open System.IO
open System.Net
open Microsoft.FSharp.Control


module TimeSeries =
    open System.Threading.Tasks
    open Microsoft.FSharp.Collections
    open Spectre.Console.Cli
    open Newtonsoft.Json
    open Npgsql
    open Output

    // config to generate a number of entries for a given day in the past
    type BackdateSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--vol")>]
        member val volume = 10000 with get, set

        [<CommandOption("-r|--rew")>]
        member val rewind: int = 32 with get, set
        
        [<CommandOption("-c|--cust")>]
        member val cst_id = Guid.ParseExact(Guid.NewGuid().ToString("N"), "N") with get, set
        


    type EmitSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--vol")>]
        member val volume = 10000 with get, set
        
        [<CommandOption("-c|--cust")>]
        member val cst_id = Guid.ParseExact(Guid.NewGuid().ToString("N"), "N") with get, set

    type EventRecord =
        { event_time: DateTime
          cst_id: Guid
          src_ip: IPAddress
          src_port: int
          dst_ip: IPAddress
          dst_port: int
          cc: string
          vpn: string
          proxy: string
          tor: string
          malware: string }
    
    type Settings = { connection: string }
    
    let convertToInet (ipStr: string) =
        IPAddress.Parse(ipStr: string)
        
    type CreateBackdatedSeries() =
        inherit Command<BackdateSettings>()
        interface ICommandLimiter<BackdateSettings>
        override _.Execute(_context, settings) =
            
            let customer = settings.cst_id

            let getConnectionString () =
                let json = File.ReadAllText("settings.json")
                
                try
                    let settings = JsonConvert.DeserializeObject<Settings>(json)
                    Some settings.connection
                with
                    | :? JsonException -> 
                        printfn "Error parsing JSON"
                        None
                        
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
                    let randomTimeStrings =
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
                        
                    let randomTimeStamps = 
                        randomTimeStrings
                        |> Array.map (fun ts -> DateTime.Parse(ts).ToUniversalTime())
                    
                    // Prevents the creating of an unnecessary array
                    randomTimeStamps
                    |> Array.sortInPlace
                    
                    // TODO: This should be a lookup of some sort - by country
                    let srcIpFirstOctets = "160.72"
                    
                    let destIpFirstOctets = 
                        match customer.ToString() with
                        | "61B2BF72EC2E450BA454D2E11591C0C6" -> "11.18"
                        | "dafe33545d454aef9f946ee47f32ca16" -> "12.19"
                        | _ -> "10.18"
                    
                    // build an array of randomized octets (3, 4) for the Source and Destination IPv4
                    let randomSrcOctets3 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString()
                        |]
                        
                    let randomSrcOctets4 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString()
                        |]

                    let randomDestOctets3 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString()
                        |]

                    let randomDestOctets4 =
                        [| for i in 0 .. (settings.volume-1)->
                                 rnd.Next(256).ToString()
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
                        
                    let randomSrcIPv4Inet = 
                        randomSrcIPv4
                        |> Array.map convertToInet

                    let randomDestIPv4 =
                        [| for i in 0 .. (settings.volume-1)->
                                 destIpFirstOctets
                                 + "."
                                 + randomDestOctets3[i]
                                 + "."
                                 + randomDestOctets4[i]       
                        |]
                        
                    let randomDestIPv4Inet = 
                        randomDestIPv4
                        |> Array.map convertToInet

                    let randomSrcPort =
                        [| for i in 0 .. (settings.volume-1)->
                                 let randomSrcPort = rnd.Next (1, 101)
                                 match randomSrcPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535)
                                 | _ -> 80
                        |]

                    let randomDestPort =
                        [| for i in 0 .. (settings.volume-1)->
                                 let randomDestPort = rnd.Next (1, 101)
                                 match randomDestPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535)
                                 | _ -> 80
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
                                 | i when i > 38 && i <= 48 -> "IN"
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
                             {   event_time = randomTimeStamps[i];
                                 cst_id = customer;
                                 src_ip = randomSrcIPv4Inet[i];
                                 src_port = randomSrcPort[i];
                                 dst_ip = randomDestIPv4Inet[i];
                                 dst_port = randomDestPort[i];
                                 cc = randomCC[i];
                                 vpn = VpnClients[i];
                                 proxy = ProxyClients[i];
                                 tor = TorClients[i];
                                 malware = MalBoolean[i]
                                 }
                        |]
                        |> Array.filter (fun record -> 
                        let eventTimeInMs = DateTimeOffset(record.event_time).ToUnixTimeMilliseconds()
                        let currentTimeInMs = DateTimeOffset(DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds()
                        eventTimeInMs < currentTimeInMs)
                        

                    // insert batch of records into Postgres using binary copy
                    match getConnectionString() with
                    | Some connectionString ->
                        use conn = new NpgsqlConnection(connectionString)
                        let copyFromRecordsToPostgresBinary (records: EventRecord[]) =
                            conn.Open()
                            use writer = conn.BeginBinaryImport("COPY events(event_time,
                                                                cst_id, src_ip, src_port,
                                                                dst_ip, dst_port, cc, vpn,
                                                                proxy, tor, malware) FROM stdin WITH BINARY")
                            for record in records do
                                writer.StartRow()
                                writer.Write(record.event_time, NpgsqlTypes.NpgsqlDbType.TimestampTz)
                                writer.Write(record.cst_id, NpgsqlTypes.NpgsqlDbType.Uuid)
                                writer.Write(record.src_ip, NpgsqlTypes.NpgsqlDbType.Inet)
                                writer.Write(record.src_port, NpgsqlTypes.NpgsqlDbType.Integer)
                                writer.Write(record.dst_ip, NpgsqlTypes.NpgsqlDbType.Inet)
                                writer.Write(record.dst_port, NpgsqlTypes.NpgsqlDbType.Integer)
                                writer.Write(record.cc, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.vpn, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.proxy, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.tor, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.malware, NpgsqlTypes.NpgsqlDbType.Text)
                            writer.Complete() |> ignore
                            conn.Close()
                        // Perform binary COPY
                        copyFromRecordsToPostgresBinary DayRecords
                        | None -> 
                            printfn "Failed to get connection string"
                            // handle error
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

            let customer = settings.cst_id
            let recordsPerMinute : int = Convert.ToInt32(Math.Round(Decimal.Divide(settings.volume, 1440), 0))
            
            let getConnectionString () =
                let filePath = "settings.json"
                let json = File.ReadAllText(filePath)
                
                try
                    let settings = JsonConvert.DeserializeObject<Settings>(json)
                    Some settings.connection
                with
                    | :? JsonException -> 
                        printfn "Error parsing JSON"
                        None

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
                    let randomTimeStrings =
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
                    let randomTimeStamps = 
                        randomTimeStrings
                        |> Array.map (fun ts -> DateTime.Parse(ts).ToUniversalTime())
                    
  
                    // TODO: This should be a lookup of some sort - by country
                    let srcIpFirstOctets = "160.72"
                    
                    let destIpFirstOctets = 
                        match customer.ToString() with
                        | "61B2BF72EC2E450BA454D2E11591C0C6" -> "11.18"
                        | "dafe33545d454aef9f946ee47f32ca16" -> "12.19"
                        | _ -> "10.18"
                    
                    // build an array of randomized octets (3, 4) for the Source and Destination IPv4
                    let randomSrcOctets3 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString()
                        |]
                        
                    let randomSrcOctets4 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString()
                        |]
    
                    let randomDestOctets3 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString()
                        |]
    
                    let randomDestOctets4 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 rnd.Next(256).ToString()
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
                        
                    let randomSrcIPv4Inet = 
                        randomSrcIPv4
                        |> Array.map convertToInet
    
                    let randomDestIPv4 =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 destIpFirstOctets
                                 + "."
                                 + randomDestOctets3[i]
                                 + "."
                                 + randomDestOctets4[i]       
                        |]
                        
                    let randomDestIPv4Inet = 
                        randomDestIPv4
                        |> Array.map convertToInet
    
                    let randomSrcPort =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 let randomSrcPort = rnd.Next (1, 101)
                                 match randomSrcPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535)
                                 | _ -> 80
                        |]
    
                    let randomDestPort =
                        [| for i in 0 .. (recordsPerMinute-1)->
                                 let randomDestPort = rnd.Next (1, 101)
                                 match randomDestPort with
                                 | i when i > 90 -> rnd.Next(1025, 65535)
                                 | _ -> 80
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
                                 | i when i > 38 && i <= 48 -> "IN"
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
                             {   event_time = randomTimeStamps[i];
                                 cst_id = customer;
                                 src_ip = randomSrcIPv4Inet[i];
                                 src_port = randomSrcPort[i];
                                 dst_ip = randomDestIPv4Inet[i];
                                 dst_port = randomDestPort[i];
                                 cc = randomCC[i];
                                 vpn = VpnClients[i];
                                 proxy = ProxyClients[i];
                                 tor = TorClients[i];
                                 malware = MalBoolean[i]
                                 }
                        |]
                    
                    match getConnectionString() with
                    | Some connectionString -> 
                        use conn = new NpgsqlConnection(connectionString)
                        let copyFromRecordsToPostgresBinary (records: EventRecord[]) =
                            conn.Open()
                            use writer = conn.BeginBinaryImport("COPY events(event_time,
                                                                cst_id, src_ip, src_port,
                                                                dst_ip, dst_port, cc, vpn,
                                                                proxy, tor, malware) FROM stdin WITH BINARY")
                            for record in records do
                                writer.StartRow()
                                writer.Write(record.event_time, NpgsqlTypes.NpgsqlDbType.TimestampTz)
                                writer.Write(record.cst_id, NpgsqlTypes.NpgsqlDbType.Uuid)
                                writer.Write(record.src_ip, NpgsqlTypes.NpgsqlDbType.Inet)
                                writer.Write(record.src_port, NpgsqlTypes.NpgsqlDbType.Integer)
                                writer.Write(record.dst_ip, NpgsqlTypes.NpgsqlDbType.Inet)
                                writer.Write(record.dst_port, NpgsqlTypes.NpgsqlDbType.Integer)
                                writer.Write(record.cc, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.vpn, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.proxy, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.tor, NpgsqlTypes.NpgsqlDbType.Text)
                                writer.Write(record.malware, NpgsqlTypes.NpgsqlDbType.Text)
                            writer.Complete() |> ignore
                            conn.Close()
                        // Perform binary COPY
                        copyFromRecordsToPostgresBinary DayRecords
                        | None -> 
                            printfn "Failed to get connection string"
                            // handle error
                                        
                    let currentCycleTime = DateTime.Now.ToString("hh:mm:ss.fff")
                    printMarkedUp $"{warn DayRecords.Length} events generated for {blue customer} at {emphasize currentCycleTime}"
                }           
            
            let sleep = 60000
            let command = "drip"
            let time = sleep / 1000
            printMarkedUp $"The {blue command} will emit events every {warn time} seconds"        
            
            task {
                while true do   
                    do! Task.Delay(sleep)
                    do! createMinuteForCompany
            }
            |> Task.WaitAll
                           
            0
