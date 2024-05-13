// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
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

    //private const long OneKb = 1024;

    //private const long OneMb = OneKb * OneKb;

    //private const long OneGb = OneKb * OneKb * OneKb;

    //[Params(OneKb, OneMb, OneGb, OneGb * 10)]
    //public long FileSize { get; set; }

    //[Params(default, 1, 2, 4, 8)]
    //public int? DegreeOfParallelism { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
        documentGenerator = new Generator(writer);
        await documentGenerator.GenerateAsync("data.txt", 1024 * 1024 * 1024);
    }

    [Benchmark]
    public async Task Sort_Document()
    {
        var sorter = new Sorter(new DocumentSorter.Configuration.DocumentSorterOptions());

        await sorter.SortAsync("data.txt", "data_sorted.txt");
    }
}