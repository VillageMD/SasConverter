[<AutoOpen>]
module Console

open System

let monitor = new obj()

/// Print on console line
let cprintf line text = 
    lock monitor (fun () -> 
        let original = Console.CursorLeft, Console.CursorTop
        Console.SetCursorPosition (0, line) 
        if String.IsNullOrWhiteSpace text then
            String.init Console.BufferWidth (fun _ -> " ") |> Console.Write // clear line
        else
            Console.Write text

        Console.SetCursorPosition original)

let cprintfn line text = 
    cprintf line <| text + Environment.NewLine
