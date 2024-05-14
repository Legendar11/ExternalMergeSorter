using DocumentGenerator.Configuration;
using System.Text;
using System.Text.RegularExpressions;

namespace DocumentGenerator.Tests
{
    [TestClass]
    public class GeneratorTests
    {
        private readonly IStringWriter writer;
        private readonly IGenerator generator;

        private const string OutputFilename = "data_test.txt";

        public GeneratorTests()
        {
            writer = new DocumentGenerator.StringWriter(new StringWriterOptions());
            generator = new Generator(writer);
        }

        [TestMethod]
        [DataRow(1024)]
        [DataRow(1024 * 2)]
        [DataRow(1024 * 1024)]
        [DataRow(1024 * 1024 * 10)]
        public async Task OutputFileSize_Equal_To_Configured(long fileSize)
        {
            await generator.GenerateAsync(new GenerateOptions
            {
                FileSize = fileSize,
                OutputFilename = OutputFilename
            });

            var generatedFile = new FileInfo(OutputFilename);

            Assert.AreEqual(fileSize, generatedFile.Length);
        }

        [TestMethod]
        [DataRow(1024 * 1024, "utf-8")]
        [DataRow(1024 * 1024, "utf-16")]
        [DataRow(1024 * 1024, "ascii")]
        public async Task OutputFileSizeWithEncoding_Equal_To_Configured(long fileSize, string encoding)
        {
            await generator.GenerateAsync(new GenerateOptions
            {
                FileSize = fileSize,
                EncodingString = encoding,
                OutputFilename = OutputFilename
            });

            var generatedFile = new FileInfo(OutputFilename);

            Assert.AreEqual(fileSize, generatedFile.Length);
        }

        [TestMethod]
        [DataRow(1024)]
        [DataRow(1024 * 2)]
        [DataRow(1024 * 1024)]
        [DataRow(1024 * 1024 * 10)]
        public async Task AtLeast_TwoString_Are_Equal(long fileSize)
        {
            await generator.GenerateAsync(new GenerateOptions
            {
                FileSize = fileSize,
                OutputFilename = OutputFilename
            });

            var hashSet = new HashSet<string>();

            using var sr = new StreamReader(OutputFilename);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    Assert.Fail("Input string is empty");
                }

                int dotIndex = line.IndexOf('.');
                var value = line[(dotIndex + 1)..];

                if (hashSet.Contains(value))
                {
                    return;
                }

                hashSet.Add(value);
            }

            Assert.Fail("Two same strings have been not found in generated file");
        }

        [TestMethod]
        [DataRow(1024, "utf-8")]
        [DataRow(1024, "utf-16")]
        [DataRow(1024, "ascii")]
        [DataRow(1024 * 1024, "utf-8")]
        [DataRow(1024 * 1024, "utf-16")]
        [DataRow(1024 * 1024, "ascii")]
        [DataRow(1024 * 1024 * 10, "utf-8")]
        [DataRow(1024 * 1024 * 10, "utf-16")]
        [DataRow(1024 * 1024 * 10, "ascii")]
        public async Task OutputFile_Has_CorrectData(long fileSize, string encoding)
        {
            await generator.GenerateAsync(new GenerateOptions
            {
                FileSize = fileSize,
                EncodingString = encoding,
                OutputFilename = OutputFilename
            });

            var regex = new Regex(@"[0-9]+\.\s\w+", RegexOptions.Compiled | RegexOptions.Singleline);

            using var sr = new StreamReader(OutputFilename, Encoding.GetEncoding(encoding));
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    Assert.Fail("Input string is empty");
                }

                Assert.IsTrue(regex.IsMatch(line));
            }
        }
    }
}