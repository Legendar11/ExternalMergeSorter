// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DocumentGenerator;
using DocumentGenerator.Configuration;
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
        IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
        documentGenerator = new Generator(writer);
        await documentGenerator.GenerateAsync("data.txt", 1024 * 1024 * 200);
    }

    [Benchmark]
    public async Task Sort_Document()
    {
        var sorter = new Sorter(new DocumentSorter.Configuration.DocumentSorterOptions());

        await sorter.SortAsync("data.txt", "data_sorted.txt");
    }
}