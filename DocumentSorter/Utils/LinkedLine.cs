namespace DocumentSorter.Utils;

// we don't need here get/set keywords, they produce additional memory allocation
internal class LinkedLine
{
    public string Value = null!;

    public int StreamReaderIndex;
}
