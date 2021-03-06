open System
open System.Threading.Tasks
open CommandLine
open DxFeed.Core.IO.Connectors
open DxFeed.Core.Parser
open DxFeed.Core.Parser.Messages

type Options =
    { [<Option('a',
               "address",
               Required = true,
               HelpText = "Address to connect for data.\n"
                          + "Supported <hostname:port> format, or path to local file.\n"
                          + "Can contains prefix file: or tcp: to specify connector.")>]
      address: string }

module MessagePrinter =
    let OnError (ex: Exception) = printfn $"{ex.Message}"
    let OnHeartbeat (message: HeartbeatMessage) = printfn $"{message}"
    let OnDescribeRecord (message: RecordDescription) = printfn $"{message}"

[<EntryPoint>]
let main argv =
    match Parser.Default.ParseArguments<Options>(argv) with
    | :? Parsed<Options> as parsed ->
        let connector =
            ConnectorFactory.CreateConnector(parsed.Value.address)

        let parser = QtpBinaryParser()

        parser.add_OnError MessagePrinter.OnError
        parser.add_OnHeartbeat MessagePrinter.OnHeartbeat
        parser.add_OnDescribeRecord MessagePrinter.OnDescribeRecord

        Task.WhenAll(parser.DoParseAsync(connector.Reader), connector.DoReceiveAsync())
        |> Async.AwaitTask
        |> Async.RunSynchronously
    | :? NotParsed<Options> as notParsed -> printfn $"{notParsed.Errors}"
    | _ -> failwith "Something went wrong"
    0
