namespace DocumentSorter.Configuration;

public class DocumentSorterOptions
{
    public long InitialChunkFileSize { get; init; } = 1024 * 1024;

    public char[] Delimeter { get; init; } = ". ".ToCharArray();

    public char[] NewLine { get; init; } = "\r\n".ToCharArray();

    public int DegreeOfParallelism { get; init; }
}
