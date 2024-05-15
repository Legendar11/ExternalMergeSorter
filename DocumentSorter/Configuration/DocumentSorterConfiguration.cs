namespace DocumentSorter.Configuration;

public class DocumentSorterConfiguration
{
    public long InitialChunkFileSize { get; init; } = 1024 * 1024;

    public char[] Delimeter { get; init; } = ". ".ToCharArray();

    public char[] NewLine { get; init; } = Environment.NewLine.ToCharArray(); // "\r\n".ToCharArray();

    public int DictionaryHashCapacity { get; init; } = 1024 * 1024 * 10;
}
