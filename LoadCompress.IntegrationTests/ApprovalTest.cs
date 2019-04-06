using System.IO;
using LoadCompress.Cli;
using NUnit.Framework;

namespace LoadCompress.IntegrationTests
{
    public class ApprovalTest
    {
        [Test]
        public void RunCli()
        {
            var testFiles = Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData"));
            var outputDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "CompressionResults");
            Directory.CreateDirectory(outputDir);
            foreach (var testFile in testFiles)
            {
                var filename = Path.GetFileName(testFile);
                var outputCompressed = Path.Combine(outputDir, Path.ChangeExtension(filename, "hex"));
                Program.Main(new []{"compress", testFile, outputCompressed, "-s"});

                var outputValidation = Path.Combine(outputDir, Path.ChangeExtension(filename, ".validate" + Path.GetExtension(filename)));
                Program.Main(new[] { "decompress", outputCompressed, outputValidation, "-s" });
            }
        }
    }
}