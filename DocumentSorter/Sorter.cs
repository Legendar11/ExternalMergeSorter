using DocumentSorter.Configuration;
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
        int? degreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        var stopWatch = new Stopwatch();

        degreeOfParallelism ??= Constants.DefaultDegreeOfParallelism;

        var fileChunks = GenerateFileChunks(inputFilename, options.InitialChunkFileSize, options.NewLine);

        var fileNames = await SeparateFileByChunks(inputFilename, fileChunks, degreeOfParallelism.Value);


        //stopWatch.Restart();
        //var dict = PrepareDictionary(inputFilename);
        //stopWatch.Stop();
        //Console.WriteLine($"Dictionary prepared for: {stopWatch.Elapsed.TotalSeconds}");

        //stopWatch.Restart();
        var comparer = new LineComparer(options.Delimeter);
        await SortInitialChunkFilesAsync(fileNames, comparer, degreeOfParallelism.Value, cancellationToken);
        fileNames = fileNames.Select(x => x.Replace("unsorted", "sorted")).ToArray();
        //stopWatch.Stop();
        //Console.WriteLine($"Files sorted for: {stopWatch.Elapsed.TotalSeconds}");

        await MergeFilesAsync(fileNames, outputFilename, comparer, degreeOfParallelism.Value, cancellationToken);

        //using (FileStream fs = new FileStream(outputFilename, FileMode.Open))
        //{
        //    fs.SetLength(fs.Length - 4);
        //}
    }


    //private Dictionary<int, int> PrepareDictionary(string file)
    //{
    //    using var reader = new StreamReader(File.OpenRead(file), Encoding.Unicode);

    //    var dict = new Dictionary<int, int>(1_000_000_000);

    //    while (!reader.EndOfStream)
    //    {
    //        var line = reader.ReadLine();
    //        dict.Add()
    //    }
    //}

    private async Task MergeFilesAsync(
        IReadOnlyCollection<string> sortedFiles,
        string outputFilename,
        IComparer<string> comparer,
        int parallellism,
        CancellationToken cancellationToken)
    {
        var iteraiton = 0;

        //const temp = "temp";

        //if (Directory.Exists("temp"))
        //{

        //}

        var stopWatch = new Stopwatch();

        var done = false;
        while (!done)
        {
            var runSize = 5;
            var finalRun = sortedFiles.Count <= runSize;

            if (finalRun)
            {
                stopWatch.Restart();
                Merge(sortedFiles, outputFilename, comparer, cancellationToken);
                stopWatch.Stop();
                Console.WriteLine($"Final Merge iteration {iteraiton} completed for: {stopWatch.Elapsed.TotalSeconds}");
                return;
            }

            var fileChunks = sortedFiles.Chunk(runSize);

            stopWatch.Restart();
            Parallel.ForEach(fileChunks.Select((f, i) => (Files: f, Index: i)), new ParallelOptions { MaxDegreeOfParallelism = parallellism }, (fileChunk) =>
            {
                var outputFilename = $"data_{iteraiton}_{fileChunk.Index}.sorted";

                if (fileChunk.Files.Length == 1)
                {
                    File.Move(fileChunk.Files[0], outputFilename);
                    return;
                }

                Merge(fileChunk.Files, outputFilename, comparer, cancellationToken);
            });
            stopWatch.Stop();
            Console.WriteLine($"Merge iteration {iteraiton} completed for: {stopWatch.Elapsed.TotalSeconds}");

            var SortedFileExtension = ".sorted";

            sortedFiles = Directory.GetFiles(Environment.CurrentDirectory, $"*{SortedFileExtension}")
                .OrderBy(x => Path.GetFileNameWithoutExtension(x))
                .ToArray();

            if (sortedFiles.Count > 1)
            {
                iteraiton++;
                continue;
            }

            done = true;
        }
    }

    private void Merge(
        IReadOnlyCollection<string> filesToMerge,
        string outputFileName,
        IComparer<string> comparer,
        CancellationToken cancellationToken)
    {
        var filesArray = filesToMerge.ToArray();

        //var streamReaders = Enumerable.Range(0, filesToMerge.Count)
        //    .Select(i => new StreamReader(File.OpenRead(filesArray[i]), Encoding.Unicode))
        //    .ToList();

        //var arrayLength = filesToMerge.Count;
        //var array = new (string? Value, int Index)[arrayLength];

        //Parallel.For(0, filesToMerge.Count, i =>
        //{
        //    array[i] = (streamReaders[i].ReadLine(), i);
        //});
        //Array.Sort(array, new RowComparer(comparer));

        using var outputSw = new StreamWriter(File.Create(outputFileName), Encoding.Unicode);

        //outputSw.WriteLine(array[0].Value);

        //var i = 0;
        //var many = 0;

        //do
        //{
        //    while (streamReaders[array[0].Index].EndOfStream)
        //    {
        //        streamReaders[array[0].Index].Dispose();

        //        var file = filesArray[array[0].Index];
        //        var temporaryFilename = $"{file}.removal";
        //        File.Move(file, temporaryFilename);
        //        File.Delete(temporaryFilename);

        //        for (i = 0; i < (arrayLength - 1); i++)
        //        {
        //            array[i] = array[i + 1];
        //        }

        //        arrayLength--;
        //        if (arrayLength == 0)
        //        {
        //            Console.WriteLine($"Many: {many}");
        //            return;
        //        }
        //    }

        //    array[0].Value = streamReaders[array[0].Index].ReadLine();

        //    i = 1;
        //    while (i < arrayLength && comparer.Compare(array[i - 1].Value, array[i].Value) > 0)
        //    {
        //        var tmp = array[i];
        //        array[i] = array[i - 1];
        //        array[i - 1] = tmp;
        //        i++;
        //    }

        //    many++;

        //    outputSw.WriteLine(array[0].Value);
        //} while (true);

        var (streamReaders, rows) = InitializeStreamReaders(filesArray).GetAwaiter().GetResult();
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

                //rows.Sort((row1, row2) => comparer.Compare(row1.Value, row2.Value));

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

        CleanupRun(streamReaders, filesArray);
    }

    private async Task<(StreamReader[] StreamReaders, List<Row> rows)> InitializeStreamReaders(IReadOnlyList<string> sortedFiles)
    {
        var streamReaders = new StreamReader[sortedFiles.Count];
        var rows = new List<Row>(sortedFiles.Count);
        for (var i = 0; i < sortedFiles.Count; i++)
        {
            var sortedFilePath = sortedFiles[i];
            var sortedFileStream = File.OpenRead(sortedFilePath);
            streamReaders[i] = new StreamReader(sortedFileStream, bufferSize: 65536);
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

    private void CleanupRun(StreamReader[] streamReaders, IReadOnlyList<string> filesToMerge)
    {
        for (var i = 0; i < streamReaders.Length; i++)
        {
            streamReaders[i].Dispose();

            // RENAME BEFORE DELETION SINCE DELETION OF LARGE FILES CAN TAKE SOME TIME
            // WE DONT WANT TO CLASH WHEN WRITING NEW FILES.

            var temporaryFilename = $"{filesToMerge[i]}.removal";
            File.Move(filesToMerge[i], temporaryFilename);
            File.Delete(temporaryFilename);
        }
    }

    private async Task SortInitialChunkFilesAsync(
        IReadOnlyCollection<string> fileNames,
        IComparer<string> comparer,
        int parallellism,
        CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = parallellism, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(fileNames, options, async (fileName, ct) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var innerStopwtch = new Stopwatch();

            var array = File.ReadAllLines(fileName, Encoding.Unicode);

            Array.Sort(array, comparer);

            File.WriteAllLines(fileName, array, Encoding.Unicode);
            File.Move(fileName, Path.ChangeExtension(fileName, "sorted"), true);
        });
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

    private async Task<IReadOnlyCollection<string>> SeparateFileByChunks(string filePath, IReadOnlyCollection<FileChunk> fileChunks, int parallellism)
    {
        var fileLength = new FileInfo(filePath).Length;
        var fileNames = new string[fileChunks.Count];

        using var file = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, $"{InputFileMapNamePrefix}_{filePath}");

        Parallel.ForEach(fileChunks, new ParallelOptions { MaxDegreeOfParallelism = parallellism }, (fileChunk) =>
        {
            using var accessor = file.CreateViewAccessor(0, fileLength);

            var fileCapacity = fileChunk.End - fileChunk.Start;

            fileNames[fileChunk.Index] = $"data_{fileChunk.Index}.unsorted";

            using var chunkFile = MemoryMappedFile.CreateFromFile(
                fileNames[fileChunk.Index],
                FileMode.Create,
                $"{ChunkFileMapNamePrefix}_{fileNames[fileChunk.Index]}",
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
