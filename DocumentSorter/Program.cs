﻿// See https://aka.ms/new-console-template for more information
using DocumentSorter.Configuration;
using DocumentSorter;
using System.Diagnostics;
using DocumentGenerator.Configuration;
using DocumentGenerator;
using System.Text;

//IStringWriter writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
//IGenerator documentGenerator = new Generator(writer);
//await documentGenerator.GenerateAsync("data.txt", 1024 * 1024 * 1024);

//var sorter = new Sorter(new DocumentSorterOptions());

//var stopwatch = Stopwatch.StartNew();
//await sorter.SortAsync("data.txt", "data_sorted.txt");
//stopwatch.Stop();
//Console.WriteLine($"Sorted for: {stopwatch.ElapsedMilliseconds / 1000.0}");