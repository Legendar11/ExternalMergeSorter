// See https://aka.ms/new-console-template for more information
using DocumentGenerator;
using DocumentGenerator.Configuration;
using System.Diagnostics;

IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
IGenerator documentGenerator = new Generator(writer);

var stopwatch = Stopwatch.StartNew();
await documentGenerator.GenerateAsync("data.txt", 1024 * 1024 * 1024);
stopwatch.Stop();

Console.WriteLine($"File is generated! Elapesed milliseconds: {stopwatch.ElapsedMilliseconds}");