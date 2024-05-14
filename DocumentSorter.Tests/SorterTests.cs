using DocumentGenerator;
using DocumentGenerator.Configuration;
using DocumentGenerator.Utils;
using DocumentSorter.Configuration;
using System.IO;

namespace DocumentSorter.Tests
{
    [TestClass]
    public class SorterTests
    {
        private readonly IStringWriter writer;
        private readonly IGenerator generator;
        private readonly ISorter sorter;
        private readonly DocumentSorterConfiguration configuration;

        private const string TempDirectory = "tests_tmp_sort";
        private string tempDirectoryPath;

        private string GenerateFilename() => Path.Combine(tempDirectoryPath, Guid.NewGuid().ToString());

        public SorterTests()
        {
            writer = new DocumentGenerator.Utils.StringWriter(new StringWriterConfiguration());
            generator = new Generator(writer);

            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
            }

            tempDirectoryPath = Directory.CreateDirectory(TempDirectory).FullName;

            configuration = new DocumentSorterConfiguration();
            sorter = new Sorter(configuration);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(TempDirectory, true);
        }

        [TestMethod]
        [DataRow(1024)]
        [DataRow(1024 * 1024)]
        [DataRow(1024 * 1024 * 10)]
        public async Task Phrases_Are_Sorted(long fileSize)
        {
            var filenameInput = GenerateFilename();
            var filenameOutput = GenerateFilename();

            await generator.GenerateAsync(new GenerateOptions
            {
                FileSize = fileSize,
                OutputFilename = filenameInput
            });

            sorter.Sort(new SortOptions
            {
                InputFileName = filenameInput,
                OutputFilename = filenameOutput
            });

            using var reader = new StreamReader(filenameOutput);

            var (_, currentPhrase) = Parse(reader.ReadLine()!);

            while (!reader.EndOfStream)
            {
                var (_, parsedPhrase) = Parse(reader.ReadLine()!);

                if (parsedPhrase != currentPhrase)
                {
                    if (string.Compare(parsedPhrase, currentPhrase) == -1)
                    {
                        Assert.Fail($"Parsed phrase {parsedPhrase} is less than {currentPhrase}");
                    }
                }
            }
        }

        [TestMethod]
        [DataRow(1024)]
        [DataRow(1024 * 1024)]
        [DataRow(1024 * 1024 * 10)]
        public async Task Numbers_Are_Sorted_InCase_Phrases_Are_Same(long fileSize)
        {
            var filenameInput = GenerateFilename();
            var filenameOutput = GenerateFilename();

            await generator.GenerateAsync(new GenerateOptions
            {
                FileSize = fileSize,
                OutputFilename = filenameInput
            });

            sorter.Sort(new SortOptions
            {
                InputFileName = filenameInput,
                OutputFilename = filenameOutput
            });

            using var reader = new StreamReader(filenameOutput);

            var (currentNumber, currentPhrase) = Parse(reader.ReadLine()!);

            while (!reader.EndOfStream)
            {
                var (parsedNumber, parsedPhrase) = Parse(reader.ReadLine()!);

                if (parsedPhrase != currentPhrase)
                {
                    currentNumber = int.MinValue;
                }
                else if (parsedNumber < currentNumber)
                {
                    Assert.Fail($"Parsed number {parsedNumber} is less than {currentNumber}");
                }
                else
                {
                    currentNumber = parsedNumber;
                }
            }
        }

        private (int Number, string Phrase) Parse(string line)
        {
            var dotIndex = line.IndexOf(configuration.Delimeter[0]);

            var number = int.Parse(line.Substring(0, dotIndex));

            var phrase = line.Substring(
                dotIndex + configuration.Delimeter.Length,
                line.Length - dotIndex - configuration.Delimeter.Length);

            return (number, phrase);
        }
    }
}