using CommandLine;
using System.Text;

namespace DocumentGenerator.Configuration;

public class GenerateOptions
{
    [Option('o', "output", Required = false, HelpText = "Set filename")]
    public string OutputFilename { get; init; } = "data.txt";

    [Option('s', "size", Required = false, HelpText = "Set file size")]
    public long FileSize { get; init; } = 1024 * 1024 * 1024;

    [Option('e', "encoding", Required = false, HelpText = "Set file encoding")]
    public string EncodingString { get; init; } = new UTF8Encoding(true).BodyName;

    public Encoding Encoding => Encoding.GetEncoding(EncodingString);

    [Option('p', "parallelism", Required = false, HelpText = "Set degree of parallelism")]
    public int DegreeOfParallelism { get; set; } = (Environment.ProcessorCount / 2) + (Environment.ProcessorCount / 4);

    [Option('f', "from", Required = false, HelpText = "Set generated numbers lowest value (included)")]
    public int GenerateFrom { get; init; } = -1_000_000;

    [Option('t', "to", Required = false, HelpText = "Set generated numbers lowest value (not included)")]
    public int GenerateTo { get; init; } = 1_000_000;
}
