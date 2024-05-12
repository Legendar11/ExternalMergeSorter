namespace DocumentGenerator;

public interface IGenerator
{
    Task GenerateAsync(
            string outputPath,
            long fileSize,
            int? degreeOfParallelism = null,
            CancellationToken cancellationToken = default);
}
