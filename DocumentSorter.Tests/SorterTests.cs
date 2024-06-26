using DocumentGenerator;
using DocumentGenerator.Configuration;
using DocumentGenerator.Utils;
using DocumentSorter.Configuration;

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
            writer = new DocumentGenerator.Utils.StringWriter();
            generator = new Generator(new DocumentGeneratorConfiguration(),writer);

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
        public async Task FileSizes_Are_Equal(long fileSize)
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

            var generatedFileInput = new FileInfo(filenameInput).Length;
            var generatedFileOutput = new FileInfo(filenameOutput).Length;

            Assert.AreEqual(fileSize, generatedFileInput);
            Assert.AreEqual(fileSize, generatedFileOutput);
            Assert.AreEqual(generatedFileInput, generatedFileOutput);
        }

        [TestMethod]
        [DataRow(1024)]
        [DataRow(1024 * 1024)]
        [DataRow(1024 * 1024 * 10)]
        public async Task AllLines_From_Generated_Exist_In_Sorted(long fileSize)
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

            var dict = new Dictionary<string, int>();

            using (var reader = new StreamReader(filenameInput))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine()!;

                    if (dict.ContainsKey(line))
                    {
                        dict[line] += 1;
                    }
                    else
                    {
                        dict[line] = 1;
                    }
                }
            }

            using (var reader = new StreamReader(filenameOutput))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine()!;

                    if (!dict.TryGetValue(line, out int value))
                    {
                        Assert.Fail($"Sorted file does not contain line from original file: {line}");
                    }
                    else
                    {
                        dict[line] = --value;
                    }
                }
            }

            Assert.IsTrue(dict.Values.All(value => value == 0), "Incosistent lines from original and sorted files");
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
        [DataRow(1024, 0, 100_000)]
        [DataRow(1024 * 1024, 0, 100_000)]
        [DataRow(1024 * 1024 * 10, 0, 100_000)]
        [DataRow(1024, -100_000, 100_000)]
        [DataRow(1024 * 1024, -100_000, 100_000)]
        [DataRow(1024 * 1024 * 10, -100_000, 100_000)]
        public async Task Numbers_Are_Sorted_InCase_Phrases_Are_Same(long fileSize, int from, int to)
        {
            var filenameInput = GenerateFilename();
            var filenameOutput = GenerateFilename();

            await generator.GenerateAsync(new GenerateOptions
            {
                FileSize = fileSize,
                OutputFilename = filenameInput,
                GenerateFrom = from,
                GenerateTo = to
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