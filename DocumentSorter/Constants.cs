namespace DocumentSorter;

public static class Constants
{
    public static int DefaultDegreeOfParallelism => (Environment.ProcessorCount / 2) + 1;

    public const int CopyBufferSize = 1024;

    public const string FileUnsortedExtension = ".unsorted";

    public const string FilelSortedExtension = ".sorted";
}
