using DocumentSorter.Configuration;
using DocumentSorter.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;
using Row = (string Value, int StreamReader);

namespace DocumentSorter;

public class Sorter(DocumentSorterOptions options)
{
    const string InputFileMapNamePrefix = "input_";
    const string ChunkFileMapNamePrefix = "chunk_";

    public async Task SortAsync(
        string inputFilename,
        string outputFilename,
        Encoding? encoding = default,
        int? degreeOfParallelism = default,
        CancellationToken cancellationToken = default)
    {
        var stopWatch = new Stopwatch();

        degreeOfParallelism ??= Constants.DefaultDegreeOfParallelism;
        encoding ??= Encoding.Default;

        var fileChunks = GenerateFileChunks(inputFilename, encoding, options.InitialChunkFileSize, options.NewLine);

        stopWatch.Restart();
        var fileNames = SeparateFileByChunks(inputFilename, encoding, fileChunks, degreeOfParallelism.Value);
        stopWatch.Stop();
        Console.WriteLine($"Chunk files created for: {stopWatch.Elapsed.TotalSeconds}");

        stopWatch.Restart();
        var comparer = new LineComparer(options.Delimeter);
        await SortInitialChunkFilesAsync(fileNames, encoding, comparer, degreeOfParallelism.Value, cancellationToken);
        fileNames = fileNames.Select(file => file.Replace(Constants.FileUnsortedExtension, Constants.FileSortedExtension)).ToArray();
        stopWatch.Stop();
        Console.WriteLine($"Files sorted for: {stopWatch.Elapsed.TotalSeconds}");

        await MergeFilesAsync(fileNames, outputFilename, encoding, comparer, degreeOfParallelism.Value, cancellationToken);

        Directory.Delete(Constants.TempDirectoryForChunkFiles, true);

        //using (FileStream fs = new FileStream(outputFilename, FileMode.Open))
        //{
        //    fs.SetLength(fs.Length - 4);
        //}
    }


    private async Task MergeFilesAsync(
        IReadOnlyCollection<string> sortedFiles,
        string outputFilename,
        Encoding encoding,
        IComparer<string> comparer,
        int parallellism,
        CancellationToken cancellationToken)
    {
        var iteraiton = 0;

        var stopWatch = new Stopwatch();

        var tmpDirectory = Path.GetDirectoryName(sortedFiles.First())!;
        var filesForSort = sortedFiles.ToArray();

        do
        {
            var fileChunks = filesForSort.Chunk(Constants.FilesPerMerge);
            stopWatch.Restart();
            Parallel.ForEach(fileChunks.Select((files, index) => (Files: files, Index: index)), new ParallelOptions { MaxDegreeOfParallelism = parallellism }, (fileChunk) =>
            {
                var outputFilenameMerged = Path.Combine(tmpDirectory, $"data_{iteraiton}_{fileChunk.Index}.{Constants.FileSortedExtension}");

                if (fileChunk.Files.Length == 1)
                {
                    File.Move(fileChunk.Files[0], outputFilenameMerged);
                    return;
                }

                Merge(fileChunk.Files, outputFilenameMerged, encoding, comparer, cancellationToken);
            });
            stopWatch.Stop();
            Console.WriteLine($"Merge iteration {iteraiton} completed for: {stopWatch.Elapsed.TotalSeconds}");

            iteraiton++;
            filesForSort = [.. Directory.GetFiles(tmpDirectory, $"*{Constants.FileSortedExtension}").OrderBy(x => Path.GetFileNameWithoutExtension(x))];
        } while (filesForSort.Length > Constants.FilesPerMerge);


        stopWatch.Restart();
        Merge(filesForSort, outputFilename, encoding, comparer, cancellationToken);
        stopWatch.Stop();
        Console.WriteLine($"Final Merge iteration {iteraiton} completed for: {stopWatch.Elapsed.TotalSeconds}");
    }

    private void Merge(
        IReadOnlyCollection<string> filesToMerge,
        string outputFileName,
        Encoding encoding,
        IComparer<string> comparer,
        CancellationToken cancellationToken)
    {
        var filesArray = filesToMerge.ToArray();

        using var outputSw = new StreamWriter(File.Create(outputFileName), encoding);

        var (streamReaders, rows) = InitializeStreamReaders(filesArray, encoding).GetAwaiter().GetResult();
        var finishedStreamReaders = new List<int>(streamReaders.Length);

        rows.Sort((row1, row2) => comparer.Compare(row1.Value, row2.Value));

        var done = false;

        while (!done)
        {
            var valueToWrite = rows[0].Value;
            var streamReaderIndex = rows[0].StreamReader;

            outputSw.WriteLine(valueToWrite);

            if (streamReaders[streamReaderIndex].EndOfStream)
            {
                var indexToRemove = rows.FindIndex(x => x.StreamReader == streamReaderIndex);
                rows.RemoveAt(indexToRemove);
                finishedStreamReaders.Add(streamReaderIndex);
                done = finishedStreamReaders.Count == streamReaders.Length;

                rows.Sort((row1, row2) => comparer.Compare(row1.Value, row2.Value));

                continue;
            }

            var value = streamReaders[streamReaderIndex].ReadLine();
            rows[0] = new Row { Value = value!, StreamReader = streamReaderIndex };

            var i = 1;
            while (i < rows.Count && comparer.Compare(rows[i - 1].Value, rows[i].Value) > 0)
            {
                var tmp = rows[i];
                rows[i] = rows[i - 1];
                rows[i - 1] = tmp;
                i++;
            }

        }

        for (var i = 0; i < streamReaders.Length; i++)
        {
            streamReaders[i].Dispose();

            var temporaryFilename = $"{filesArray[i]}.removal";
            File.Move(filesArray[i], temporaryFilename);
            File.Delete(temporaryFilename);
        }
    }

    private async Task<(StreamReader[] StreamReaders, List<Row> rows)> InitializeStreamReaders(IReadOnlyList<string> sortedFiles, Encoding encoding)
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


    private async Task SortInitialChunkFilesAsync(
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

    private static IReadOnlyCollection<FileChunk> GenerateFileChunks(string filePath, Encoding encoding, long chunkSize, char[] newLine)
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

    private static IReadOnlyCollection<string> SeparateFileByChunks(string filePath, Encoding encoding, IReadOnlyCollection<FileChunk> fileChunks, int parallellism)
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

        using var file = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, $"{InputFileMapNamePrefix}_{filePath}");

        Parallel.ForEach(fileChunks, parallelOptions, (fileChunk) =>
        {
            using var accessor = file.CreateViewAccessor(0, fileLength);

            var fileCapacity = fileChunk.End - fileChunk.Start;

            fileNames[fileChunk.Index] = Path.Combine(tempDirectoryPath, $"data_{fileChunk.Index}.{Constants.FileUnsortedExtension}");

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
