// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DocumentGenerator;
using DocumentGenerator.Configuration;
using DocumentGenerator.Utils;
using DocumentSorter;
using DocumentSorter.Configuration;

BenchmarkRunner.Run<DocumentSorterBenchmaks>();

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.SlowestToFastest)]
public class DocumentSorterBenchmaks
{
    private DocumentGenerator.IGenerator documentGenerator = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        IStringWriter writer = new DocumentGenerator.Utils.StringWriter();
        documentGenerator = new Generator(new DocumentGeneratorConfiguration(), writer);
        await documentGenerator.GenerateAsync(new GenerateOptions());
    }

    [Benchmark]
    public void Sort_Document()
    {
        var sorter = new Sorter(new DocumentSorterConfiguration());

        sorter.Sort(new SortOptions());
    }
}