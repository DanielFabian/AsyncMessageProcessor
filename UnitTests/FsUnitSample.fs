module UnitTests.Tests
open Xunit
open FsUnit.Xunit
open FsCheck
open System.Diagnostics.Contracts

[<Property>]
let ``try using random data`` (xs:list<float>) =
    Contract.Requires (xs != null, "xs")
    List.rev (List.rev xs) = xs

[<Fact>]
let Test () = ()

[<Fact>]
let ``when I ask whether it is On it answers true`` ()= ()

[<Fact>]
let ``when I convert it to a string it becomes "On"`` ()= ()
            

        