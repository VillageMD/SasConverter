[<AutoOpen>]
module CommandLine

open System
open FSharp.Data.Toolbox.SasFile.SasToText

let rec parseCmdLine options argv = 
    let argv' = List.map 
                 (fun (arg: string) -> arg.TrimStart([| '-'; '/' |]).ToLowerInvariant() ) 
                 argv
    match argv' with
    | [] -> options
    | "sourcepath"::path::tail
    | "s"::path::tail -> 
        parseCmdLine { options with SourcePath = path } tail 
    | "outputdir"::dir::tail
    | "o"::dir::tail -> 
        parseCmdLine { options with OutputDir = dir } tail 
    | "dateformat"::d::tail
    | "d"::d::tail -> 
        let parsed, _ = DateTime.TryParse d
        if parsed then 
            parseCmdLine { options with DateFormat = d } tail 
        else
            printfn "Couldn't parse date format '%s'" d
            parseCmdLine options tail 
    | "timeformat"::t::tail
    | "t"::t::tail -> 
        let parsed, _ = DateTime.TryParse t
        if parsed then 
            parseCmdLine { options with TimeFormat = t } tail 
        else
            printfn "Couldn't parse time format '%s'" t
            parseCmdLine options tail 
    | "dateandtimeformat"::dt::tail
    | "datetimeformat"::dt::tail
    | "dt"::dt::tail -> 
        let parsed, _ = DateTime.TryParse dt
        if parsed then 
            parseCmdLine { options with DateAndTimeFormat = dt } tail 
        else
            printfn "Couldn't parse datetime format '%s'" dt
            parseCmdLine options tail 
    | ukn::tail -> printfn "Unrecognized option '%s'" ukn
                   parseCmdLine options tail
