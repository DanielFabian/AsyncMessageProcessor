// Learn more about F# at http://fsharp.net
open System
open System.Reactive.Linq
open System.Threading

type TimeAsyncResult(timeout : TimeSpan, numBytes, userCallback : AsyncCallback) as this =
    let startTime = DateTime.Now
    let waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset)
    let notifyWaitHandle value = 
        waitHandle.Set() |> ignore
        userCallback.Invoke(this)
    let timer = Observable.Timer(timeout)
    do
        timer.Subscribe notifyWaitHandle |> ignore
    interface IAsyncResult with
        member x.AsyncState = raise (NotImplementedException())
        member x.IsCompleted = raise (NotImplementedException())
        member x.AsyncWaitHandle = raise (NotImplementedException())
        member x.CompletedSynchronously = raise (NotImplementedException())
    member x.NumBytes = numBytes

type FakePipe() = 
    let random = Random(0)
    member x.BeginRead (buffer : byte array) (offset : int) (length : int) (userCallback : System.AsyncCallback) (stateObject : obj) : IAsyncResult = 
        let numBytes = random.Next(length - offset)
        let data = Array.zeroCreate numBytes
        random.NextBytes(data) |> ignore
        Array.Copy (data, 0, buffer, offset, numBytes) |> ignore
        new TimeAsyncResult(TimeSpan.FromMilliseconds(random.NextDouble() * 1000.0), numBytes, userCallback) :> IAsyncResult
    member x.EndRead (asyncResult : IAsyncResult) : int = 
        let timeAsyncResult = asyncResult :?> TimeAsyncResult 
        timeAsyncResult.NumBytes

let asyncRead (pipe : FakePipe) buffer = 
    Observable.FromAsyncPattern (pipe.BeginRead buffer 0 1024, pipe.EndRead)

let whenRead pipe buffer = 
    Observable.Defer(asyncRead pipe buffer).Repeat()

let printToScreen (buffer : byte array) numBytes =
    printfn "%d bytes received: " numBytes
    let string = Convert.ToBase64String (buffer, 0, numBytes)
    printfn "%s" string

[<EntryPoint>]
let main args =
    printfn "Starting the trouble"

    let buffer = Array.zeroCreate 1024
    let pipe = FakePipe()
    (whenRead pipe buffer).SubscribeOn(new System.Reactive.Concurrency.EventLoopScheduler()).Subscribe(printToScreen buffer) |> ignore

    Console.ReadKey() |> ignore
    0