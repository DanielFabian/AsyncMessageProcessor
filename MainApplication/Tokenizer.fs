namespace MainApplication

open System.Reactive.Linq

type Tokenizer() =
    let mutable currentBuffer = [| |]
    let tokenize chunk =
        currentBuffer <- Array.append currentBuffer chunk
        let rec tokenize() = 
            match Array.tryFindIndex (fun byte -> byte = 0uy) currentBuffer with
                | Some(index) -> let firstMessage = currentBuffer.[..index]
                                 currentBuffer <- currentBuffer.[index+1..]
                                 rx {
                                        yield firstMessage
                                        yield! tokenize()
                                    }
                | None -> Observable.Empty()
        tokenize()
    member x.Tokenize = tokenize
