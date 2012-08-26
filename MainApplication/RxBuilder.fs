[<AutoOpen>]
module rxBuilder
open System
open System.Linq
open System.Reactive.Linq
 

type rxBuilder() =    
    member this.Bind ((xs:'a IObservable), (f:'a -> 'b IObservable)) =
        Observable.SelectMany (xs, f)
    member this.Delay (f:Func<IObservable<_>>) = Observable.Defer f
    member this.Return x = Observable.Return x
    member this.ReturnFrom xs = xs
    member this.Combine (xs:'a IObservable, ys: 'a IObservable) =
        Observable.Concat (xs, ys)
    member this.For (xs : 'a seq, f: 'a -> 'b IObservable) =
        Observable.SelectMany(xs.ToObservable(), new Func<_, IObservable<_>>(f))
    member this.TryFinally (xs: 'a IObservable, f : unit -> unit) =
        Observable.Finally(xs, new Action(f))
    member this.TryWith (xs: 'a IObservable, f: exn -> 'a IObservable) =
        Observable.Catch (xs, new Func<exn, 'a IObservable>(f))
    member this.While (f, xs: 'a IObservable) =
        Observable.TakeWhile (xs, new Func<_,bool>(fun _ -> f()))
    member this.Yield x = Observable.Return x
    member this.YieldFrom xs = xs
    member this.Zero () = Observable.Empty()
    member this.Using(res:#IDisposable, body) = this.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())
              
let rx = rxBuilder()