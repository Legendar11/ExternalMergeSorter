using DocumentGenerator.Extensions;
using System.IO.MemoryMappedFiles;

namespace DocumentGenerator;

public class Generator(IStringWriter writer) : IGenerator
{
    private const string NewFileMapNamePrefix = "generated_";

    public async Task GenerateAsync(
        string outputPath,
        long fileSize,
        int? degreeOfParallelism = default,
        CancellationToken cancellationToken = default)
    {
        using var file = MemoryMappedFile.CreateFromFile(
                outputPath,
                FileMode.Create,
                $"{NewFileMapNamePrefix}_{outputPath}",
                fileSize);

        degreeOfParallelism ??= CalcluateDegreeOfParallelism(fileSize);

        var accessors = CreateFileAccessors(file, fileSize, degreeOfParallelism.Value);
        var indexedAccessors = accessors.Select((accessor, i) => (Accessor: accessor, Index: i));

        await Parallel.ForEachAsync(indexedAccessors, cancellationToken, (indexedAccessor, ct) =>
        {
            var accessor = indexedAccessor.Accessor;
            var isLastAccessor = indexedAccessor.Index == (accessors.Length - 1);

            var positionInView = 0L;
            var newPositionInView = 0L;

            var buffer = new char[Constants.LineBufferSize];
            var bufferPosition = 0;

            while (positionInView < accessor.Capacity)
            {
                ct.ThrowIfCancellationRequested();

                writer.WriteLine(buffer, ref bufferPosition);

                var isWritten = accessor.TryWriteCharArray(positionInView, buffer, 0, bufferPosition, out newPositionInView);
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

            var remainingSymbolsToWrite = (int)(accessor.Capacity - positionInView) / sizeof(char);

            if (positionInView == 0)
            {
                if (!isLastAccessor)
                {
                    bufferPosition -= writer.Options.NewLine.Length;
                    writer.WriteNewLine(buffer, ref bufferPosition);
                }

                // write as many symbols as fitted into available space
                accessor.WriteArray(positionInView, buffer, 0, remainingSymbolsToWrite);
            }
            else
            {
                // because we didn't write the last line - 
                // we need to return to previous line
                positionInView -= writer.Options.NewLine.Length * sizeof(char);

                // in case last line of the file - we don't need to add new line, so we can use spare symbols
                if (isLastAccessor)
                {
                    remainingSymbolsToWrite += writer.Options.NewLine.Length;
                    bufferPosition = 0;
                }

                // add extra symbols to fullfill the available space
                writer.WriteRandomSymbols(remainingSymbolsToWrite, buffer, ref bufferPosition);

                if (!isLastAccessor)
                {
                    writer.WriteNewLine(buffer, ref bufferPosition);
                    remainingSymbolsToWrite += writer.Options.NewLine.Length;
                }

                // add extra symbols to the last line of an accessor
                accessor.WriteArray(positionInView, buffer, 0, remainingSymbolsToWrite);
            }

            return new ValueTask();
        });

        foreach (var accessor in accessors)
        {
            accessor.Dispose();
        }
    }

    private static int CalcluateDegreeOfParallelism(long fileSize) => fileSize switch
    {
        <= (1024 * 1024) => 1, // 1 MB
        _ => Constants.DefaultDegreeOfParallelism
    };

    private static MemoryMappedViewAccessor[] CreateFileAccessors(MemoryMappedFile file, long fileSize, int count)
    {
        var viewSize = fileSize / count;

        if (viewSize % sizeof(char) != 0)
        {
            viewSize++;
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
