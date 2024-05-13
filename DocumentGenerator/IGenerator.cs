using System.Text;

namespace DocumentGenerator;

public interface IGenerator
{
    Task GenerateAsync(
            string outputPath,
            long fileSize,
            Encoding? encoding = default,
            int? degreeOfParallelism = default,
            CancellationToken cancellationToken = default);
}
