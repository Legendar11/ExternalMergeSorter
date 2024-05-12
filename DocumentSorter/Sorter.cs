using DocumentSorter.Configuration;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentSorter;

public class Sorter(DocumentSorterOptions options)
{
    const string InputFileMapNamePrefix = "input_";

    public async Task Sort(string inputFilename, CancellationToken cancellationToken = default)
    {
        var fileLength = new FileInfo(inputFilename).Length;

        var fileChunks = GenerateFileChunks(inputFilename, options.InitialChunkFileSize, options.Delimeter);
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
            endPosition += fileLength / filesCount;

            positionInFile = endPosition;

            do
            {
                // check end
                accessor.ReadArray(positionInFile, buffer, 0, buffer.Length);
                positionInFile += 1;

                if ((positionInFile + 4) >= accessor.Capacity)
                {
                    throw new Exception();
                }
            } while (!Enumerable.SequenceEqual(newLine, buffer));

            positionInFile--;
            endPosition = positionInFile;

            fileChunks[currentFileIndex] = new FileChunk(currentFileIndex, startPosition, endPosition);

            currentFileIndex++;
            startPosition = endPosition + newLine.Length * 2;
        }

        fileChunks[currentFileIndex] = new FileChunk(currentFileIndex, startPosition, fileLength);

        return fileChunks;
    }
}
