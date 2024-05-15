using CommandLine;
using System.Text;

namespace DocumentSorter.Configuration;

public class SortOptions
{
    [Option('i', "input", Required = false, HelpText = "Set input filename")]
    public string InputFileName { get; init; } = "data.txt";

    [Option('o', "output", Required = false, HelpText = "Set output filename")]
    public string OutputFilename { get; init; } = "data_sorted.txt";

    [Option('e', "encoding", Required = false, HelpText = "Set file encoding")]
    public string? EncodingString { get; init; } = null;

    public Encoding Encoding =>
        !string.IsNullOrWhiteSpace(EncodingString)
        ? Encoding.GetEncoding(EncodingString)
        : Encoding.Default;

    [Option('p', "parallelism", Required = false, HelpText = "Set degree of parallelism")]
    public int DegreeOfParallelism { get; set; } = (Environment.ProcessorCount / 2) + (Environment.ProcessorCount / 4);

    [Option('m', "merge", Required = false, HelpText = "Set how many files will be merged per iteration")]
    public  int FilesPerMerge { get; init; } = 5;

    [Option('g', "generate", Required = false, HelpText = "In case is provided - file with provided size will be generated")]
    public long? FileSizeToGenerate { get; init; } = 1024;
}