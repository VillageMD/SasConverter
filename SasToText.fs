namespace FSharp.Data.Toolbox.SasFile

open System
open System.IO
open FSharp.Collections.ParallelSeq

module SasToText =

    type ConversionOptions = {
        SourcePath        : string // can be single file, directory, or mask
        OutputDir         : string

        ColumnDelimiter   : string
        RowDelimiter      : string
        TextQualifier     : string
        NullValue         : string

        WriteHeader       : bool
        DateFormat        : string
        TimeFormat        : string
        DateAndTimeFormat : string
        Trim              : bool
        TrimEnd           : bool

        DOP               : int // degree of parallelism
        GZip              : bool
        }
        with static member Default = {
                SourcePath        = Environment.CurrentDirectory
                OutputDir         = Environment.CurrentDirectory
                ColumnDelimiter   = "\t"
                RowDelimiter      = Environment.NewLine
                TextQualifier     = "\""
                NullValue         = String.Empty
                WriteHeader       = true
                DateFormat        = "yyyy-MM-dd"
                TimeFormat        = "HH:mm:ss"
                DateAndTimeFormat = "yyyy-MM-ddTHH:mm:ss"
                Trim              = true
                TrimEnd           = true
                DOP               = 1
                GZip              = false
            }

    let Convert options =

        let srcDir = 
            try
                let srcAttr = File.GetAttributes options.SourcePath
                if srcAttr.HasFlag FileAttributes.Directory then options.SourcePath
                else Path.GetDirectoryName options.SourcePath
            with
            | :? ArgumentException as exn when exn.Message = "Illegal characters in path." -> 
                Path.GetDirectoryName options.SourcePath

        if not <| Directory.Exists srcDir then
            failwith "Source directory not found: '%s'" srcDir
        if not <| Directory.Exists options.OutputDir then
            failwith "Output directory not found: '%s'" options.OutputDir

        let progressLine = Console.CursorTop
        let mutable finished = 0
        let total = Directory.GetFiles(srcDir, Path.GetFileName options.SourcePath).Length
        let mask = 
            if total = 0 then "*.sas7bdat"
            else Path.GetFileName options.SourcePath

        let total = Directory.GetFiles(srcDir, mask).Length

        Directory.EnumerateFiles(srcDir, mask)
        |> Seq.mapi (fun i v -> i, v)
        |> PSeq.withDegreeOfParallelism options.DOP
        |> PSeq.iter (fun (n, filename) ->
//            let converting = sprintf "Converting '%s'..." <| Path.GetFileName filename
//            let lineNumber = progressLine + n + 1
//            cprintfn lineNumber converting

            let txtFilename =
                Path.Combine(
                    options.OutputDir,
                    Path.GetFileNameWithoutExtension filename + ".txt")

            use sasFile = new SasFile(filename)
            use writer = File.CreateText txtFilename

            // write header
            if options.WriteHeader then
                sasFile.MetaData.Columns
                |> List.map (fun col -> col.Name)
                |> String.concat options.ColumnDelimiter
                |> writer.Write
                writer.Write options.RowDelimiter

            // write lines
            sasFile.Rows()
            |> Seq.iter (fun row ->
                let line =
                    row
                    |> Seq.map (fun value ->
                        match value with
                        | Number n -> n.ToString()
                        | Character s ->
                            let str = 
                                if options.Trim then
                                    s.Trim()
                                elif options.TrimEnd then
                                    s.TrimEnd()
                                else s
                            if String.IsNullOrEmpty s then options.NullValue
                            elif s.Contains(options.ColumnDelimiter) then
                                options.TextQualifier + 
                                    s.Replace(options.TextQualifier, 
                                        options.TextQualifier + options.TextQualifier) + 
                                    options.TextQualifier
                            else s
                        | Time t -> t.ToString options.TimeFormat
                        | Date d -> d.ToString options.DateFormat
                        | DateAndTime dt -> dt.ToString options.DateAndTimeFormat
                        | Empty -> options.NullValue
                        )
                    |> String.concat options.ColumnDelimiter
                if not <| String.IsNullOrEmpty line then
                    writer.Write line
                    writer.Write options.RowDelimiter
                )
            //cprintf lineNumber <| converting + "done."
            finished <- finished + 1
            cprintf progressLine <| sprintf "Converted %i of %i files" finished total
            )
        printfn ""
