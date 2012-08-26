open MainApplication
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
        new TimeAsyncResult(TimeSpan.FromMilliseconds(random.NextDouble() * 100.0), numBytes, userCallback) :> IAsyncResult
    member x.EndRead (asyncResult : IAsyncResult) : int = 
        let timeAsyncResult = asyncResult :?> TimeAsyncResult 
        timeAsyncResult.NumBytes

let asyncRead (pipe : FakePipe) buffer = 
    Observable.FromAsyncPattern (pipe.BeginRead buffer 0 buffer.Length, pipe.EndRead)

let whenRead pipe buffer = 
    Observable.Defer(asyncRead pipe buffer).Repeat()

let printToScreen (buffer : byte array) =
    printfn "%d bytes received: " buffer.Length
    let string = Convert.ToBase64String buffer
    printfn "%s" string 

let formatData bytes =
    let unsortedString = Convert.ToBase64String bytes
    let unsortedChars = Array.ofSeq unsortedString
    String (Array.sort unsortedChars)
    
let printFormatedToScreen (formatedMessage : string) =
    printfn "%d characters long formated message: " formatedMessage.Length
    printfn "%s" formatedMessage

type Command =
    | Digit of int
    | Char of char
    | Complex of string * int

let (|Digit|_|) (str : string) = 
    if Char.IsDigit (str.[0]) then Some(Digit(int str.[0] - int '0'))
    else None
let (|Char|_|) (str : string) = 
    if str.Length >= 5 && Char.IsLetter (str.[5]) then Some(Char(str.[5]))
    else None
let (|Complex|_|) (str : string) = 
    if str.StartsWith "++" then
        let initialPlus = Seq.takeWhile (fun char -> char = '+') str |> Array.ofSeq
        Some(Complex(String(initialPlus), str.Length))
    else None

let parseMessage message =
    match message with
    | Digit(digit) -> sprintf "%A" (Digit(digit))
    | Char(char) -> sprintf "%A" (Char(char))
    | Complex(pluses, length) -> sprintf "%A" (Complex(pluses, length))
    | _ -> "It's baaad luck to be you..."

[<EntryPoint>]
let main args =
    printfn "Starting the trouble"

    let buffer = Array.zeroCreate 100
    let pipe = FakePipe()
    let messageParts = (whenRead pipe buffer).SubscribeOn(new System.Reactive.Concurrency.EventLoopScheduler()).Select(fun numBytes -> buffer.[..numBytes-1])
    let tokenizer = Tokenizer()
    let messages = messageParts.SelectMany tokenizer.Tokenize
    //messages.Subscribe printToScreen |> ignore
    let formatedMessages = messages.Select formatData
    //formatedMessages.Subscribe printFormatedToScreen |> ignore
    let parsedMessages = formatedMessages.Select parseMessage
    parsedMessages.Subscribe printFormatedToScreen |> ignore
    Console.ReadKey() |> ignore
    printfn "\nDone trouble-making"
    0