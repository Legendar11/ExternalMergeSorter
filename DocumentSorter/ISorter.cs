using DocumentSorter.Configuration;

namespace DocumentSorter;

public interface ISorter
{
    void Sort(SortOptions options, CancellationToken cancellationToken = default);
}
