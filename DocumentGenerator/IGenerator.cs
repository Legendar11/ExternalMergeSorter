using DocumentGenerator.Configuration;

namespace DocumentGenerator;

public interface IGenerator
{
    Task GenerateAsync(GenerateOptions options, CancellationToken cancellationToken = default);
}
