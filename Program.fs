open System
open System.IO
open System.Configuration

open FSharp.Data.Toolbox.Sas.SasToText

open FSharp.Configuration

type Settings = AppSettings<"app.config">

[<EntryPoint>]
let main argv = 
    let options = 
        argv
        |> List.ofArray
        |> parseCmdLine 
            {
                SourcePath        = Settings.SourcePath
                OutputDir         = Settings.OutputDir
                ColumnDelimiter   = Settings.ColumnDelimiter
                RowDelimiter      = Settings.RowDelimiter
                TextQualifier     = Settings.TextQualifier
                NullValue         = Settings.NullValue
                WriteHeader       = Settings.WriteHeader
                DateFormat        = Settings.DateFormat
                TimeFormat        = Settings.TimeFormat
                DateAndTimeFormat = Settings.DateAndTimeFormat 
                Trim              = Settings.Trim
                TrimEnd           = Settings.TrimEnd
                DOP               = Settings.DOP
                GZip              = Settings.GzIp
            }

    let dflt = ConversionOptions.Default
    let options = if options.SourcePath = "" then {options with SourcePath = dflt.SourcePath } else options
    let options = if options.OutputDir = "" then {options with OutputDir = dflt.OutputDir } else options
    let options = 
        let parsed, _ = DateTime.TryParse(options.DateFormat)
        if parsed then options
        else {options with DateFormat = dflt.DateFormat}
    let options = 
        let parsed, _ = DateTime.TryParse(options.TimeFormat)
        if parsed then options
        else {options with TimeFormat = dflt.TimeFormat}
    let options = 
        let parsed, _ = DateTime.TryParse(options.DateAndTimeFormat)
        if parsed then options
        else {options with DateAndTimeFormat = dflt.DateAndTimeFormat}
    let options = 
        if options.DOP < 1 then {options with DOP = dflt.DOP} else options

    let timer = new Diagnostics.Stopwatch()
    timer.Start()

    Convert options

    timer.ElapsedMilliseconds / 1000L
    |> float
    |> TimeSpan.FromSeconds
    |> string
    |> printfn "Total time: %s"


    0