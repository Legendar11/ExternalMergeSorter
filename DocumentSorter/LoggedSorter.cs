﻿using DocumentSorter.Configuration;
using System.Diagnostics;
using System.Text;

namespace DocumentSorter;

internal class LoggedSorter(DocumentSorterOptions options) : Sorter(options)
{
    private readonly Stopwatch stopwatch = new();

    protected override IReadOnlyCollection<FileChunk> GenerateFileChunks(
        string filePath,
        Encoding encoding,
        long chunkSize,
        char[] newLine)
    {
        stopwatch.Restart();
        var result = base.GenerateFileChunks(filePath, encoding, chunkSize, newLine);
        stopwatch.Stop();
        return result;
    }

    protected override IReadOnlyCollection<string> SeparateFileByChunks(
        string filePath,
        Encoding encoding,
        IReadOnlyCollection<FileChunk> fileChunks,
        int parallellism)
    {
        stopwatch.Restart();
        var result = base.SeparateFileByChunks(filePath, encoding, fileChunks, parallellism);
        stopwatch.Stop();
        Console.WriteLine($"Files sorted for: {stopwatch.Elapsed.TotalSeconds}");
        return result;
    }

    protected override async Task SortInitialChunkFilesAsync(
        IReadOnlyCollection<string> fileNames,
        Encoding encoding,
        IComparer<string> comparer,
        int parallellism,
        CancellationToken cancellationToken)
    {
        stopwatch.Restart();
        await base.SortInitialChunkFilesAsync(fileNames, encoding, comparer, parallellism, cancellationToken);
        stopwatch.Stop();
        Console.WriteLine($"Chunk files created for: {stopwatch.Elapsed.TotalSeconds}");
    }

    protected override void MergeSortedFilesIntoOne(
        IReadOnlyCollection<string> sortedFiles,
        string outputFilename,
        Encoding encoding,
        IComparer<string> comparer,
        int parallellism,
        int filesPerMerge,
        CancellationToken cancellationToken)
    {
        stopwatch.Restart();
        base.MergeSortedFilesIntoOne(sortedFiles, outputFilename, encoding, comparer, parallellism, filesPerMerge, cancellationToken);
        stopwatch.Stop();
        Console.WriteLine($"General merge process completed for: {stopwatch.Elapsed.TotalSeconds}");
    }

    protected override void MergeFileChunk(
        IReadOnlyCollection<string> filesToMerge,
        string outputFileName,
        Encoding encoding,
        IComparer<string> comparer,
        CancellationToken cancellationToken)
    {
        stopwatch.Restart();
        base.MergeFileChunk(filesToMerge, outputFileName, encoding, comparer, cancellationToken);
        stopwatch.Stop();
        Console.WriteLine($"Merge iteration completed for: {stopwatch.Elapsed.TotalSeconds}");
    }

}