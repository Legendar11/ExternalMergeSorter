using DocumentSorter.Configuration;
using DocumentSorter.Extensions;
using DocumentSorter.Utils;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace DocumentSorter;

public class Sorter(DocumentSorterConfiguration configuration) : ISorter
{
    public void Sort(SortOptions options, CancellationToken cancellationToken = default)
    {
        var fileChunks = GenerateFileChunks(
            options.InputFileName,
            options.Encoding,
            configuration.InitialChunkFileSize,
            configuration.NewLine);

        var fileNames = SeparateFileByChunks(
            options.InputFileName,
            options.Encoding,
            fileChunks,
            options.DegreeOfParallelism);

        var dictHash = new ConcurrentDictionary<int, int>(options.DegreeOfParallelism, configuration.DictionaryHashCapacity);
        var comparer = new LineComparer(configuration.Delimeter, dictHash);

        SortInitialChunkFiles(
            fileNames,
            options.Encoding,
            comparer,
            options.DegreeOfParallelism,
            cancellationToken);

        fileNames = fileNames
            .Select(file => file.Replace(Constants.FileUnsortedExtension, Constants.FileSortedExtension))
            .ToArray();

        MergeSortedFilesIntoOne(
            fileNames,
            options.OutputFilename,
            options.Encoding,
            comparer,
            options.DegreeOfParallelism,
            options.FilesPerMerge,
            cancellationToken);

        Directory.Delete(Constants.TempDirectoryForChunkFiles, recursive: true);
    }

    protected virtual void MergeSortedFilesIntoOne(
        IReadOnlyCollection<string> sortedFiles,
        string outputFilename,
        Encoding encoding,
        IComparer<string> comparer,
        int parallellism,
        int filesPerMerge,
        CancellationToken cancellationToken)
    {
        var iteraiton = 0;

        var tmpDirectory = Path.GetDirectoryName(sortedFiles.First())!;
        var filesForSort = sortedFiles.ToArray();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallellism, CancellationToken = cancellationToken };

        while (filesForSort.Length > filesPerMerge)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileChunks = filesForSort
                .Chunk(filesPerMerge)
                .Select((files, index) =>
                (
                    Files: files,
                    Index: index,
                    Output: Path.Combine(tmpDirectory, $"data_{iteraiton}_{index}.{Constants.FileSortedExtension}")
                ));

            Parallel.ForEach(fileChunks, parallelOptions, (fileChunk) =>
            {
                if (fileChunk.Files.Length == 1)
                {
                    File.Move(fileChunk.Files[0], fileChunk.Output);
                }
                else
                {
                    MergeFileChunk(fileChunk.Files, fileChunk.Output, encoding, comparer, cancellationToken);
                }
            });

            iteraiton++;
            filesForSort = Directory.GetFiles(tmpDirectory, $"*{Constants.FileSortedExtension}");
        }

        MergeFileChunk(filesForSort, outputFilename, encoding, comparer, cancellationToken);
    }

    protected virtual void MergeFileChunk(
        IReadOnlyCollection<string> filesToMerge,
        string outputFileName,
        Encoding encoding,
        IComparer<string> comparer,
        CancellationToken cancellationToken)
    {
        var filesArray = filesToMerge.ToArray();

        using var outputSw = new StreamWriter(File.Create(outputFileName), encoding);

        var streamReaders = Enumerable.Range(0, filesArray.Length)
            .Select(i => new StreamReader(File.OpenRead(filesArray[i]), encoding))
            .ToArray();

        var lines = Enumerable.Range(0, filesArray.Length)
            .Select(i => new LinkedLine
            {
                StreamReaderIndex = i,
                Value = streamReaders[i].ReadLine()!
            })
            .ToList();

        var finishedStreamReadersCount = 0;

        lines.Sort((row1, row2) => comparer.Compare(row1.Value, row2.Value));

        while (finishedStreamReadersCount < streamReaders.Length)
        {
            outputSw.WriteLine(lines[0].Value);

            var currentStreamReaderIndex = lines[0].StreamReaderIndex;
            if (streamReaders[currentStreamReaderIndex].EndOfStream)
            {
                finishedStreamReadersCount++;

                SafeRemoval(streamReaders[currentStreamReaderIndex], filesArray[currentStreamReaderIndex]);

                var indexToRemove = lines.FindIndex(x => x.StreamReaderIndex == currentStreamReaderIndex);

                lines.RemoveAt(0);

                continue;
            }

            var value = streamReaders[currentStreamReaderIndex].ReadLine();
            lines[0].Value = value!;
            lines[0].StreamReaderIndex = currentStreamReaderIndex;

            var i = 1;
            while (i < lines.Count && comparer.Compare(lines[i - 1].Value, lines[i].Value) > 0)
            {
                (lines[i - 1], lines[i]) = (lines[i], lines[i - 1]);
                i++;
            }
        }
    }

    private static void SafeRemoval(StreamReader reader, string filename)
    {
        reader.Dispose();

        var temporaryFilename = $"{filename}.removal";
        File.Move(filename, temporaryFilename);
        File.Delete(temporaryFilename);
    }

    protected virtual void SortInitialChunkFiles(
        IReadOnlyCollection<string> fileNames,
        Encoding encoding,
        IComparer<string> comparer,
        int parallellism,
        CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = parallellism, CancellationToken = cancellationToken };

        Parallel.ForEach(fileNames, options, fileName =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var array = File.ReadAllLines(fileName, encoding);

            Array.Sort(array, comparer);

            File.WriteAllLines(fileName, array, encoding);
            File.Move(fileName, Path.ChangeExtension(fileName, "sorted"), true);
        });
    }

    protected virtual IReadOnlyCollection<FileChunk> GenerateFileChunks(string filePath, Encoding encoding, long chunkSize, char[] newLine)
    {
        var fileLength = new FileInfo(filePath).Length;

        var filesCount = (int)Math.Ceiling(fileLength / (double)chunkSize);

        var fileChunks = new FileChunk[filesCount];

        var sizeOfSymbolInBytes = encoding.GetSymbolSize();

        var newLineBytesCount = encoding.GetByteCount(newLine);
        var newLineEncoded = encoding.GetBytes(newLine);

        var currentFileIndex = 0;
        var startPosition = 0L;
        var endPosition = 0L;
        var positionInFile = 0L;
        var buffer = new byte[newLineBytesCount];

        using var file = MemoryMappedFile.CreateFromFile(
            filePath,
            FileMode.Open,
            null);
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

                if (positionInFile >= accessor.Capacity)
                {
                    throw new Exception("File has incorrect format: new line has not been detected");
                }
            } while (!Enumerable.SequenceEqual(newLineEncoded, buffer));

            positionInFile -= sizeOfSymbolInBytes;
            endPosition = positionInFile;

            fileChunks[currentFileIndex] = new FileChunk(currentFileIndex, startPosition, endPosition);

            currentFileIndex++;
            startPosition = endPosition + newLineBytesCount;
        }

        fileChunks[currentFileIndex] = new FileChunk(currentFileIndex, startPosition, fileLength);

        return fileChunks;
    }

    protected virtual IReadOnlyCollection<string> SeparateFileByChunks(string filePath, Encoding encoding, IReadOnlyCollection<FileChunk> fileChunks, int parallellism)
    {
        var fileLength = new FileInfo(filePath).Length;
        var fileNames = new string[fileChunks.Count];

        var sizeOfSymbolInBytes = encoding.GetSymbolSize();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallellism };

        if (Directory.Exists(Constants.TempDirectoryForChunkFiles))
        {
            Directory.Delete(Constants.TempDirectoryForChunkFiles, true);
        }

        var tempDirectoryPath = Directory.CreateDirectory(Constants.TempDirectoryForChunkFiles).FullName;

        using var file = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null);

        Parallel.ForEach(fileChunks, parallelOptions, (fileChunk) =>
        {
            using var accessor = file.CreateViewAccessor(0, fileLength);

            var fileCapacity = fileChunk.End - fileChunk.Start;

            fileNames[fileChunk.Index] = Path.Combine(
                tempDirectoryPath,
                $"data_{fileChunk.Index}.{Constants.FileUnsortedExtension}");

            File.Create(fileNames[fileChunk.Index]).Close();

            using var chunkFile = MemoryMappedFile.CreateFromFile(
                   fileNames[fileChunk.Index],
                   FileMode.Open,
                   null,
                   fileCapacity);
            using var chunkAccessor = chunkFile.CreateViewAccessor(0, fileCapacity);

            var buffer = new byte[Constants.CopyBufferSize];
            var readedBytes = 0;
            var position = fileChunk.Start;
            var miniPosition = 0;
            var leftCapacity = 0;

            do
            {
                readedBytes = accessor.ReadArray(position, buffer, 0, buffer.Length);

                leftCapacity = (int)(fileCapacity - miniPosition);

                if (leftCapacity < readedBytes)
                {
                    if (leftCapacity != 0)
                    {
                        chunkAccessor.WriteArray(miniPosition, buffer, 0, leftCapacity);
                    }
                    break;
                }

                chunkAccessor.WriteArray(miniPosition, buffer, 0, readedBytes);

                position += readedBytes;
                miniPosition += readedBytes;
            } while (readedBytes > 0 && position < accessor.Capacity);
        });

        return fileNames;
    }
}
