using DocumentGenerator.Configuration;
using DocumentGenerator.Extensions;
using DocumentGenerator.Utils;
using System.IO.MemoryMappedFiles;

namespace DocumentGenerator;

public class Generator(DocumentGeneratorConfiguration configuration, IStringWriter writer) : IGenerator
{
    private static Random random = new();

    public async Task GenerateAsync(GenerateOptions options, CancellationToken cancellationToken = default)
    {
        options.DegreeOfParallelism = options.FileSize > (1024 * 1024)
            ? options.DegreeOfParallelism
            : 1;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.DegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        var sizeOfSymbolInBytes = options.Encoding.GetByteCount("0");

        File.Create(options.OutputFilename).Close();

        using var file = MemoryMappedFile.CreateFromFile(
                options.OutputFilename,
                FileMode.Open,
                null,
                options.FileSize);

        var accessors = CreateFileAccessors(
            file,
            options.FileSize,
            sizeOfSymbolInBytes,
            options.DegreeOfParallelism);

        await Parallel.ForEachAsync(accessors, parallelOptions, (accessor, ct) =>
        {
            var positionInView = 0L;
            var newPositionInView = 0L;

            var buffer = new char[Constants.LineBufferSize];
            var bufferPosition = 0;

            var bufferBytes = new byte[Constants.LineBufferSize * sizeOfSymbolInBytes];
            var bufferBytesPosition = 0;
            var encodedBytesCount = 0;

            int randomNumber = 0;
            char[] randomPhrase;

            while (positionInView < accessor.Capacity)
            {
                ct.ThrowIfCancellationRequested();

                randomNumber = random.Next(options.GenerateFrom, options.GenerateTo);
                writer.WriteNumber(randomNumber, buffer, ref bufferPosition);

                writer.WriteSymbols(configuration.Delimeter, buffer, ref bufferPosition);

                randomPhrase = Constants.Phrases[random.Next(Constants.Phrases.Length)];
                writer.WriteSymbols(randomPhrase, buffer, ref bufferPosition);

                writer.WriteSymbols(configuration.NewLine, buffer, ref bufferPosition);

                encodedBytesCount = options.Encoding.GetBytes(
                    buffer,
                    0,
                    bufferPosition,
                    bufferBytes,
                    bufferBytesPosition);

                var isWritten = accessor.TryWriteByteArray(
                    positionInView,
                    bufferBytes,
                    0,
                    encodedBytesCount,
                    out newPositionInView);

                if (isWritten)
                {
                    positionInView = newPositionInView;
                    bufferPosition = 0;
                }
                else
                {
                    break;
                }
            }

            var remainingSymbolsToWrite = (int)(accessor.Capacity - positionInView) / sizeOfSymbolInBytes;

            if (positionInView == 0)
            {
                bufferPosition -= configuration.NewLine.Length;
                writer.WriteSymbols(configuration.NewLine, buffer, ref bufferPosition);

                // write as many symbols as fitted into available space
                accessor.WriteArray(positionInView, buffer, 0, remainingSymbolsToWrite);
            }
            else
            {
                // because we didn't write the last line - 
                // we need to return to previous line
                positionInView -= configuration.NewLine.Length * sizeOfSymbolInBytes;

                // add extra symbols to fullfill the available space
                bufferPosition = 0;
                writer.WriteRandomSymbols(remainingSymbolsToWrite, buffer, ref bufferPosition);

                // append a new line
                writer.WriteSymbols(configuration.NewLine, buffer, ref bufferPosition);
                remainingSymbolsToWrite += configuration.NewLine.Length;

                // add extra symbols to the last line of an accessor
                encodedBytesCount = options.Encoding.GetBytes(
                    buffer,
                    0,
                    remainingSymbolsToWrite,
                    bufferBytes,
                    bufferBytesPosition);
                accessor.WriteArray(positionInView, bufferBytes, 0, encodedBytesCount);
            }

            return new ValueTask();
        });

        foreach (var accessor in accessors)
        {
            accessor.Dispose();
        }
    }

    private static MemoryMappedViewAccessor[] CreateFileAccessors(
        MemoryMappedFile file,
        long fileSize,
        int sizeOfSymbolInBytes,
        int count)
    {
        var viewSize = fileSize / count;

        if (viewSize % sizeOfSymbolInBytes != 0)
        {
            viewSize += viewSize % sizeOfSymbolInBytes;
        }

        var accessors = new MemoryMappedViewAccessor[count];
        var lastIndex = count - 1;
        for (long i = 0; i < lastIndex; i++)
        {
            accessors[i] = file.CreateViewAccessor(i * viewSize, viewSize);
        }
        var remainingSpace = fileSize - lastIndex * viewSize;
        accessors[lastIndex] = file.CreateViewAccessor(lastIndex * viewSize, remainingSpace);

        return accessors;
    }
}
