// See https://aka.ms/new-console-template for more information
using CommandLine;
using DocumentGenerator;
using DocumentGenerator.Configuration;
using DocumentGenerator.Utils;
using DocumentSorter;
using DocumentSorter.Configuration;
using System.Diagnostics;
using StringWriter = DocumentGenerator.Utils.StringWriter;

var options = Parser.Default.ParseArguments<SortOptions>(args);
if (options.Errors.Any())
{
    return;
}

Console.WriteLine("Document sorter will be executed with next options:");
Console.WriteLine($"\t{nameof(SortOptions.InputFileName)}: {options.Value.InputFileName}");
Console.WriteLine($"\t{nameof(SortOptions.OutputFilename)}: {options.Value.OutputFilename}");
Console.WriteLine($"\t{nameof(SortOptions.DegreeOfParallelism)}: {options.Value.DegreeOfParallelism}");
Console.WriteLine($"\t{nameof(SortOptions.Encoding)}: {options.Value.Encoding.EncodingName}");
Console.WriteLine($"\t{nameof(SortOptions.FilesPerMerge)}: {options.Value.FilesPerMerge}");
Console.WriteLine($"\t{nameof(SortOptions.FileSizeToGenerate)}: {options.Value.FileSizeToGenerate.ToString() ?? "-"}");

if (options.Value.FileSizeToGenerate != null)
{
    IStringWriter writer = new StringWriter(new StringWriterConfiguration());
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

ISorter sorter = new Sorter(new DocumentSorterConfiguration());

var stopwatch = Stopwatch.StartNew();
await sorter.SortAsync(options.Value);
stopwatch.Stop();
Console.WriteLine($"File is sorted! Elapesed seconds: {stopwatch.Elapsed.TotalSeconds}");