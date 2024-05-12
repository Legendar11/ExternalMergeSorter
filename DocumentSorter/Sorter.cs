using DocumentSorter.Configuration;
using System.IO.MemoryMappedFiles;

namespace DocumentSorter;

public class Sorter(DocumentSorterOptions options)
{
    const string InputFileMapNamePrefix = "input_";
    const string ChunkFileMapNamePrefix = "chunk_";

    public async Task Sort(string inputFilename, CancellationToken cancellationToken = default)
    {
        var fileChunks = GenerateFileChunks(inputFilename, options.InitialChunkFileSize, options.Delimeter);

        var fileNames = SeparateFileByChunks(inputFilename, fileChunks);
    }


    private static IReadOnlyCollection<FileChunk> GenerateFileChunks(string filePath, long chunkSize, char[] newLine)
    {
        var fileLength = new FileInfo(filePath).Length;

        var filesCount = (int)Math.Ceiling(fileLength / (double)chunkSize);

        var fileChunks = new FileChunk[filesCount];

        var currentFileIndex = 0;
        var startPosition = 0L;
        var endPosition = 0L;
        var positionInFile = 0L;
        var buffer = new char[newLine.Length];

        using var file = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, $"{InputFileMapNamePrefix}_{filePath}");
        using var accessor = file.CreateViewAccessor(0, fileLength);

        while (currentFileIndex < (filesCount - 1))
        {
            endPosition += fileLength / filesCount; // check is round

            positionInFile = endPosition;

            do
            {
                // check end
                accessor.ReadArray(positionInFile, buffer, 0, buffer.Length);
                positionInFile += 1;

                if ((positionInFile + 4) >= accessor.Capacity) //
                {
                    throw new Exception();
                }
            } while (!Enumerable.SequenceEqual(newLine, buffer));

            positionInFile--;
            endPosition = positionInFile;

            fileChunks[currentFileIndex] = new FileChunk(currentFileIndex, startPosition, endPosition);

            currentFileIndex++;
            startPosition = endPosition + newLine.Length * sizeof(char);
        }

        fileChunks[currentFileIndex] = new FileChunk(currentFileIndex, startPosition, fileLength);

        return fileChunks;
    }

    private IReadOnlyCollection<string> SeparateFileByChunks(string filePath, IReadOnlyCollection<FileChunk> fileChunks, int? parallellism = default)
    {
        var fileLength = new FileInfo(filePath).Length;
        var fileNames = new string[fileChunks.Count];

        parallellism ??= Constants.DefaultDegreeOfParallelism;

        using var file = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, $"{InputFileMapNamePrefix}_{filePath}");

        Parallel.ForEach(fileChunks, new ParallelOptions { MaxDegreeOfParallelism = parallellism.Value }, fileChunk =>
        {
            using var accessor = file.CreateViewAccessor(0, fileLength);

            var fileCapacity = fileChunk.End - fileChunk.Start;

            fileNames[fileChunk.Index] = $"data_{fileChunk.Index}.unsorted";

            using var chunkFile = MemoryMappedFile.CreateFromFile(
                fileNames[fileChunk.Index],
                FileMode.Create,
                $"{ChunkFileMapNamePrefix}_{filePath}",
                fileCapacity);
            using var chunkAccessor = chunkFile.CreateViewAccessor(0, fileCapacity);

            var buffer = new char[Constants.CopyBufferSize];
            var readedBytes = 0;
            var position = fileChunk.Start;
            var miniPosition = 0;
            var leftCapacity = 0;

            do
            {
                readedBytes = accessor.ReadArray(position, buffer, 0, buffer.Length);

                leftCapacity = (int)(fileCapacity - miniPosition) / sizeof(char);

                if (leftCapacity < readedBytes)
                {
                    chunkAccessor.WriteArray(miniPosition, buffer, 0, leftCapacity);
                    break;
                }

                chunkAccessor.WriteArray(miniPosition, buffer, 0, readedBytes);

                position += readedBytes;
                miniPosition += readedBytes;
            } while (readedBytes > 0);
        });

        return fileNames;
    }
}
