using DocumentSorter.Configuration;
using DocumentSorter.Extensions;
using DocumentSorter.Utils;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace DocumentSorter;

public class Row
{
    public string Value = null!;

    public int StreamReader;
}

public class Sorter(DocumentSorterConfiguration configuration) : ISorter
{
    public async Task SortAsync(SortOptions options, CancellationToken cancellationToken = default)
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

        await SortInitialChunkFilesAsync(
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
            var fileChunks = filesForSort
                .Chunk(filesPerMerge)
                .Select((files, index) =>
                (
                    Files: files,
                    Index: index,
                    Output: Path.Combine(tmpDirectory, $"data_{iteraiton}_{index}.{Constants.FileSortedExtension}")
                ));

            Parallel.ForEach(fileChunks, (fileChunk) =>
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

        var (streamReaders, rows) = InitializeStreamReaders(filesArray, encoding).GetAwaiter().GetResult();
        var finishedStreamReadersCount = 0;

        rows.Sort((row1, row2) => comparer.Compare(row1.Value, row2.Value));

        while (finishedStreamReadersCount < streamReaders.Length)
        {
            var valueToWrite = rows[0].Value;
            var streamReaderIndex = rows[0].StreamReader;

            outputSw.WriteLine(valueToWrite);

            if (streamReaders[streamReaderIndex].EndOfStream)
            {
                var indexToRemove = rows.FindIndex(x => x.StreamReader == streamReaderIndex);

                streamReaders[streamReaderIndex].Dispose();

                var temporaryFilename = $"{filesArray[streamReaderIndex]}.removal";
                File.Move(filesArray[streamReaderIndex], temporaryFilename);
                File.Delete(temporaryFilename);

                rows.RemoveAt(indexToRemove);

                finishedStreamReadersCount++;

                rows.Sort((row1, row2) => comparer.Compare(row1.Value, row2.Value));

                continue;
            }

            var value = streamReaders[streamReaderIndex].ReadLine();
            rows[0] = new Row { Value = value!, StreamReader = streamReaderIndex };

            var i = 1;
            while (i < rows.Count && comparer.Compare(rows[i - 1].Value, rows[i].Value) > 0)
            {
                (rows[i - 1], rows[i]) = (rows[i], rows[i - 1]);
                i++;
            }
        }
    }

    protected async Task<(StreamReader[] StreamReaders, List<Row> rows)> InitializeStreamReaders(IReadOnlyList<string> sortedFiles, Encoding encoding)
    {
        var streamReaders = new StreamReader[sortedFiles.Count];
        var rows = new List<Row>(sortedFiles.Count);
        for (var i = 0; i < sortedFiles.Count; i++)
        {
            var sortedFilePath = sortedFiles[i];
            var sortedFileStream = File.OpenRead(sortedFilePath);
            streamReaders[i] = new StreamReader(sortedFileStream, encoding);
            var value = await streamReaders[i].ReadLineAsync();
            var row = new Row
            {
                Value = value!,
                StreamReader = i
            };
            rows.Add(row);
        }

        return (streamReaders, rows);
    }

    protected virtual async Task SortInitialChunkFilesAsync(
        IReadOnlyCollection<string> fileNames,
        Encoding encoding,
        IComparer<string> comparer,
        int parallellism,
        CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = parallellism, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(fileNames, options, async (fileName, ct) =>
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

        const string InputFileMapNamePrefix = "input_";
        using var file = MemoryMappedFile.CreateFromFile(
            filePath,
            FileMode.Open,
            $"{InputFileMapNamePrefix}_{filePath}");
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

        const string InputFileMapNamePrefix = "input_";
        using var file = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, $"{InputFileMapNamePrefix}_{filePath}");

        Parallel.ForEach(fileChunks, parallelOptions, (fileChunk) =>
        {
            using var accessor = file.CreateViewAccessor(0, fileLength);

            var fileCapacity = fileChunk.End - fileChunk.Start;

            fileNames[fileChunk.Index] = Path.Combine(tempDirectoryPath, $"data_{fileChunk.Index}.{Constants.FileUnsortedExtension}");

            const string ChunkFileMapNamePrefix = "chunk_";
            using var chunkFile = MemoryMappedFile.CreateFromFile(
                   fileNames[fileChunk.Index],
                   FileMode.Create,
                   $"{ChunkFileMapNamePrefix}_{fileChunk.Index}",
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

                leftCapacity = (int)(fileCapacity - miniPosition) / sizeOfSymbolInBytes;

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
