using DocumentGenerator.Configuration;

namespace DocumentGenerator;

public interface IGenerator
{
    Task GenerateAsync(Options options, CancellationToken cancellationToken = default);
}
