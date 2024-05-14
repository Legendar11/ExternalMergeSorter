namespace DocumentGenerator.Configuration;

public class StringWriterConfiguration
{
    public char[] Delimeter { get; init; } = ". ".ToCharArray();

    public char[] NewLine { get; init; } = "\r\n".ToCharArray();
}
