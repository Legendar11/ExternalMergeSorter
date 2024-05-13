// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DocumentGenerator;
using DocumentGenerator.Configuration;
using System.Text;

BenchmarkRunner.Run<DocumentGeneratorBenchmaks>();

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class DocumentGeneratorBenchmaks
{
    private IGenerator documentGenerator = null!;

    private const string OutputPath = "data_benchmark_temp.txt";

    [Params("1 Kb", "1 Mb", "1 Gb", "10 Gb")]
    public string FileSize { get; set; } = null!;

    [Params("1", "4", "8", "Max")]
    public string DegreeOfParallelism { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
        documentGenerator = new Generator(writer);
    }

    [Benchmark]
    public async Task Generate_Document()
    {
        var fileSize = FileSize switch
        {
            "1 Kb" => 1024,
            "1 Mb" => 1024 * 1024,
            "1 Gb" => 1024 * 1024 * 1024,
            "10 Gb" => 1024 * 1024 * 1024 * 10L,
            _ => throw new ArgumentException()
        };
        var parallelism = DegreeOfParallelism switch
        {
            "1" => 1,
            "4" => 4,
            "8" => 8,
            "Max" => default,
            _ => throw new ArgumentException()
        };

        await documentGenerator.GenerateAsync(OutputPath, fileSize, Encoding.Default, parallelism, default);
    }
}