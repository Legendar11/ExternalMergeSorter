using CommandLine;
using DocumentGenerator;
using DocumentGenerator.Configuration;
using System.Diagnostics;

var options = Parser.Default.ParseArguments<Options>(args);
if (options.Errors.Any())
{
    return;
}
if (options.Value.FileSize <= 1024 && options.Value.DegreeOfParallelism != 1)
{
    options.Value.DegreeOfParallelism = 1;
    Console.WriteLine("For files less than or equal 1Kb degree of parallelism = 1");
}

Console.WriteLine("Document generator will be executed with next options:");
Console.WriteLine($"\t{nameof(Options.OutputPath)}: {options.Value.OutputPath}");
Console.WriteLine($"\t{nameof(Options.FileSize)}: {options.Value.FileSize}");
Console.WriteLine($"\t{nameof(Options.DegreeOfParallelism)}: {options.Value.DegreeOfParallelism}");
Console.WriteLine($"\t{nameof(Options.Encoding)}: {options.Value.Encoding.EncodingName}");

IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
IGenerator documentGenerator = new Generator(writer);

var stopwatch = Stopwatch.StartNew();
await documentGenerator.GenerateAsync(options.Value);
stopwatch.Stop();

Console.WriteLine($"File is generated! Elapesed seconds: {stopwatch.Elapsed.TotalSeconds}");