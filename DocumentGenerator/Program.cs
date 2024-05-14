using CommandLine;
using DocumentGenerator;
using DocumentGenerator.Configuration;
using DocumentGenerator.Utils;
using System.Diagnostics;
using StringWriter = DocumentGenerator.Utils.StringWriter;

var options = Parser.Default.ParseArguments<GenerateOptions>(args);
if (options.Errors.Any())
{
    return;
}
if (options.Value.FileSize <= (1024 * 1024) && options.Value.DegreeOfParallelism != 1)
{
    Console.WriteLine("For files less than or equal 1 Mb degree of parallelism = 1");
}
if (options.Value.GenerateFrom >= options.Value.GenerateTo)
{
    Console.WriteLine("Provided range is incorrect");
}
if (Math.Abs(options.Value.GenerateFrom - options.Value.GenerateTo) < 10)
{
    Console.WriteLine("Range difference at minimum 10");
}

Console.WriteLine("Document generator will be executed with next options:");
Console.WriteLine($"\t{nameof(GenerateOptions.OutputFilename)}: {options.Value.OutputFilename}");
Console.WriteLine($"\t{nameof(GenerateOptions.FileSize)}: {options.Value.FileSize}");
Console.WriteLine($"\t{nameof(GenerateOptions.DegreeOfParallelism)}: {options.Value.DegreeOfParallelism}");
Console.WriteLine($"\t{nameof(GenerateOptions.Encoding)}: {options.Value.Encoding.EncodingName}");
Console.WriteLine($"\t{nameof(GenerateOptions.GenerateFrom)}: {options.Value.GenerateFrom}");
Console.WriteLine($"\t{nameof(GenerateOptions.GenerateTo)}: {options.Value.GenerateTo}");

IStringWriter writer = new StringWriter(new StringWriterConfiguration());
IGenerator documentGenerator = new Generator(writer);

var stopwatch = Stopwatch.StartNew();
await documentGenerator.GenerateAsync(options.Value);
stopwatch.Stop();

Console.WriteLine($"File is generated! Elapesed seconds: {stopwatch.Elapsed.TotalSeconds}");