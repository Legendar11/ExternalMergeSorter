// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DocumentGenerator;
using DocumentSorter;

BenchmarkRunner.Run<DocumentSorterBenchmaks>();

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.SlowestToFastest)]
public class DocumentSorterBenchmaks
{
    private DocumentGenerator.IGenerator documentGenerator = null!;

    private const string OutputPath = "data_benchmark_temp.txt";

    [GlobalSetup]
    public async Task Setup()
    {
        IStringWriter writer = new DocumentGenerator.StringWriter(new DocumentGenerator.Configuration.StringWriterOptions());
        documentGenerator = new Generator(writer);
        await documentGenerator.GenerateAsync(new DocumentGenerator.Configuration.Options());
    }

    [Benchmark]
    public async Task Sort_Document()
    {
        var sorter = new Sorter(new DocumentSorter.Configuration.DocumentSorterOptions());

        await sorter.SortAsync("data.txt", "data_sorted.txt");
    }
}