namespace DocumentGenerator.Configuration;

public class DocumentGeneratorConfiguration
{
    public char[] Delimeter { get; init; } = ". ".ToCharArray();

    public char[] NewLine { get; init; } = Environment.NewLine.ToCharArray(); //"\r\n".ToCharArray();
}
