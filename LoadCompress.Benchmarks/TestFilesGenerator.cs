using System.IO;
using System.Text;

namespace LoadCompress.Benchmarks
{
    public static class TestFilesGenerator
    {
        public static int Generate(string filename, int words)
        {
            int bytesCount = 0;
            var lorem = new Bogus.DataSets.Lorem();
            using (var file = File.CreateText(filename))
            {
                for (var i = 0; i < words; i++)
                {
                    var word = lorem.Word();
                    file.Write(word);
                    bytesCount += Encoding.UTF8.GetByteCount(word);
                }
            }

            return bytesCount;
        }
    }
}