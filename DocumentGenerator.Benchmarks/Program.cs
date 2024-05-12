// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DocumentGenerator;
using DocumentGenerator.Configuration;
using static BenchmarkDotNet.Attributes.MarkdownExporterAttribute;

BenchmarkRunner.Run<DocumentGeneratorBenchmaks>();

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.SlowestToFastest)]
public class DocumentGeneratorBenchmaks
{
    private IGenerator documentGenerator = null!;

    private const string OutputPath = "data_benchmark_temp.txt";

    private const long OneKb = 1024;

    private const long OneMb = OneKb * OneKb;

    private const long OneGb = OneKb * OneKb * OneKb;

    [Params(OneKb, OneMb, OneGb, OneGb * 10)]
    public long FileSize { get; set; }

    [Params(default, 1, 2, 4, 8)]
    public int? DegreeOfParallelism { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
        documentGenerator = new Generator(writer);
    }

    [Benchmark]
    public async Task Generate_Document()
    {
        await documentGenerator.GenerateAsync(OutputPath, FileSize, DegreeOfParallelism);
    }
}