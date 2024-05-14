namespace DocumentGenerator.Configuration;

public class StringWriterConfiguration
{
    public int GenerateNumberFrom { get; init; } = 0;

    public int GenerateNumberTo { get; init; } = 1_000_000;

    public char[] Delimeter { get; init; } = ". ".ToCharArray();

    public char[] NewLine { get; init; } = "\r\n".ToCharArray();
}
