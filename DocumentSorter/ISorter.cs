using DocumentSorter.Configuration;

namespace DocumentSorter;

public interface ISorter
{
    Task SortAsync(SortOptions options, CancellationToken cancellationToken = default);
}
