namespace Commands

open System
open System.IO
open System.Text.Json
open MathNet.Numerics
open FSharp.Data
open FSharp.Json
open Microsoft.FSharp.Collections

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

    // config to generate a number of entries for a given day in the past
    type BackdateSettings() =
        inherit CommandSettings()

        [<CommandOption("-v|--volume")>]
        member val volume = 100 with get, set

        [<CommandOption("-r|--rewind")>]
        member val rewind: int = 1 with get, set

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

    // TODO: use this record to manage generation of the JSON object
    type EventRecord =
        { EventTime: string
          src_ip: string
          src_port: string
          dst_ip: string
          dst_port: string
          cc: string
          vpn: string
          proxy: string
          tor: string
          malware: string }

    type CreateBackdatedSeries() =
        inherit Command<BackdateSettings>()
        interface ICommandLimiter<BackdateSettings>

        override _.Execute(_context, settings) =
            // set up random functions
            let rnd = Random()
            // let shuffleR (r : Random) xs = xs |> Seq.sortBy (fun _ -> r.Next())
            // TODO: create a for loop to cycle through all days up to the present
            let dateInPast =
                DateTime.Now.AddDays(-(settings.rewind))

            let RewindDate =
                dateInPast.ToString("MM/dd/yyyy")
            // build a list of randomized time values for hour, minute, second and millis (0 padded)
            let randomHours =
                [ for i in 0 .. settings.volume do
                      rnd.Next(23).ToString().PadLeft(2, '0') ]

            let randomMinutes =
                [ for i in 0 .. settings.volume do
                      rnd.Next(59).ToString().PadLeft(2, '0') ]

            let randomSeconds =
                [ for i in 0 .. settings.volume do
                      rnd.Next(59).ToString().PadLeft(2, '0') ]

            let randomMillis =
                [ for i in 0 .. settings.volume do
                      rnd.Next(999).ToString().PadLeft(3, '0') ]
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

            // build a list of randomized ports for source and destination IPs
            let randomSrcPort =
                MathNet.Numerics.Combinatorics.GeneratePermutation(100)

            let randomSrcPort =
                [ for i in 0 .. settings.volume do
                      if randomSrcPort[i] > 90 then
                          rnd.Next(1025, 65535).ToString()
                      else
                          "80" ]

            let randomDestPort =
                MathNet.Numerics.Combinatorics.GeneratePermutation(100)

            let randomDestPort =
                [ for i in 0 .. settings.volume do
                      if randomDestPort[i] > 90 then
                          rnd.Next(1025, 65535).ToString()
                      else
                          "80" ]

            // TODO: Create a case for choosing countries by numerical range
            // let randomCC = MathNet.Numerics.Combinatorics.GeneratePermutation(100)
            // create list of countries to select for sample data :: for now repeats increase chance of selection
            let CountryList =
                [ "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "RU"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "CN"
                  "UK"
                  "UK"
                  "UK"
                  "UK"
                  "US"
                  "US"
                  "US"
                  "US"
                  "US"
                  "US"
                  "IT"
                  "CA"
                  "MX" ]

            // generate list of countries
            let randomCC =
                [ for i in 0 .. settings.volume do
                      CountryList.[rnd.Next(CountryList.Length)] ]

            // create list of VPNs clients to select for sample data :: repeats increase chance of selection
            let VPNList =
                [ "nord"
                  "nord"
                  "nord"
                  "nord"
                  "nord"
                  "nord"
                  "nord"
                  "nord"
                  "nord"
                  "nord"
                  "surfshark"
                  "surfshark"
                  "surfshark"
                  "surfshark"
                  "surfshark"
                  "surfshark"
                  "foxyproxy"
                  "foxyproxy"
                  "foxyproxy"
                  "foxyproxy"
                  "foxyproxy"
                  "foxyproxy"
                  "proton"
                  "proton"
                  "proton"
                  "proton"
                  "proton"
                  "proton"
                  "proton"
                  "proton"
                  "purevpn"
                  "purevpn"
                  "purevpn"
                  "nord;proton"
                  "nord;surfshark"
                  "nord;foxyproxy" ]
            // create list of unique values between 1 and 100
            let randomVPN =
                MathNet.Numerics.Combinatorics.GeneratePermutation(100)
            // select top 20%ish of values
            let VpnClientList =
                [ for i in 0 .. settings.volume do
                      if randomVPN[i] > 80 then
                          VPNList.[rnd.Next(VPNList.Length)]
                      else
                          "" ]
            // create list of unique values between 1 and 100
            let randomProxy =
                MathNet.Numerics.Combinatorics.GeneratePermutation(100)
            // select top 20%ish of values - use VpnClientList value if present, otherwise get a new value
            let ProxyClientList =
                [ for i in 0 .. settings.volume do
                      if randomProxy[i] > 80 then
                          if VpnClientList[i] <> "" then
                              VpnClientList[i]
                          else
                              VPNList.[rnd.Next(VPNList.Length)]
                      else
                          "" ]
            // create list of unique values between 1 and 100
            let randomTor =
                MathNet.Numerics.Combinatorics.GeneratePermutation(100)
            // select top 20%ish of values - use VpnClientList or ProxyClientList value, otherwise get new
            let TorClientList =
                [ for i in 0 .. settings.volume do
                      if randomTor[i] > 80 then
                          if (VpnClientList[i] <> "" || ProxyClientList[i] <> "") then
                              if VpnClientList[i] <> "" then
                                  VpnClientList[i]
                              else
                                  ProxyClientList[i]
                          else
                              VPNList.[rnd.Next(VPNList.Length)]
                      else
                          "" ]


            // set up a list for MAL booleans
            let randomMAL =
                MathNet.Numerics.Combinatorics.GeneratePermutation(100)

            let MalBoolean =
                [ for i in 0 .. settings.volume do
                      if randomMAL[i] > 80 then
                          "TRUE"
                      else
                          "FALSE" ]

            let DayRecordList =
                [ for i in 0 .. settings.volume do
                      "{ EventTime: "
                      + randomTimeStamps[i]
                      + ", src_ip: "
                      + randomSrcIPv4[i]
                      + ", src_port: "
                      + randomSrcPort[i]
                      + ", dst_ip: "
                      + randomDestIPv4[i]
                      + ", dst_port: "
                      + randomDestPort[i]
                      + ", cc: "
                      + randomCC[i]
                      + ", vpn: "
                      + VpnClientList[i]
                      + ", proxy: "
                      + ProxyClientList[i]
                      + ", tor: "
                      + TorClientList[i]
                      + ", malware: "
                      + MalBoolean[i]
                      + " }" ]

            let DayRecordJSON =
                JsonSerializer.Serialize DayRecordList

            //let fileDateTime =
           //     DateTime.Now.ToString("yyyy-MM-ddd=hh-mm-ss_")

            File.WriteAllText("EventData.json", DayRecordJSON)
            printf "%A" DayRecordJSON
            printfn ""
            printMarkedUp $"You've set rewind for {emphasize RewindDate} and to generate {info settings.volume} events!"
            0
