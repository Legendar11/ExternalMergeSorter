﻿// See https://aka.ms/new-console-template for more information
using CommandLine;
using DocumentGenerator;
using DocumentGenerator.Configuration;
using DocumentSorter;
using DocumentSorter.Configuration;
using System.Diagnostics;

var options = Parser.Default.ParseArguments<SortOptions>(args);
if (options.Errors.Any())
{
    return;
}

Console.WriteLine("Document generator will be executed with next options:");
Console.WriteLine($"\t{nameof(SortOptions.InputFileName)}: {options.Value.InputFileName}");
Console.WriteLine($"\t{nameof(SortOptions.OutputFilename)}: {options.Value.OutputFilename}");
Console.WriteLine($"\t{nameof(SortOptions.DegreeOfParallelism)}: {options.Value.DegreeOfParallelism}");
Console.WriteLine($"\t{nameof(SortOptions.Encoding)}: {options.Value.Encoding.EncodingName}");
Console.WriteLine($"\t{nameof(SortOptions.FilesPerMerge)}: {options.Value.FilesPerMerge}");
Console.WriteLine($"\t{nameof(SortOptions.FileSizeToGenerate)}: {options.Value.FileSizeToGenerate.ToString() ?? "-"}");

if (options.Value.FileSizeToGenerate != null)
{
    IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
    IGenerator documentGenerator = new Generator(writer);
    await documentGenerator.GenerateAsync(new GenerateOptions
    {
        OutputFilename = options.Value.InputFileName,
        FileSize = options.Value.FileSizeToGenerate.Value,
        EncodingString = options.Value.EncodingString,
        DegreeOfParallelism = options.Value.DegreeOfParallelism
    });
    Console.WriteLine($"File with size {options.Value.FileSizeToGenerate} is generated.");
}
else if (!File.Exists(options.Value.InputFileName))
{
    Console.WriteLine($"File '{options.Value.InputFileName}' is not found!");
    return;
}

ISorter sorter = new LoggedSorter(new DocumentSorterOptions());

var stopwatch = Stopwatch.StartNew();
await sorter.SortAsync(new SortOptions());
stopwatch.Stop();
Console.WriteLine($"File is sorted! Elapesed seconds: {stopwatch.Elapsed.TotalSeconds}");