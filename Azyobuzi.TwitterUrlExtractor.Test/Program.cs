using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Azyobuzi.TwitterUrlExtractor.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var extractor = new Extractor();
            foreach (var test in LoadTests())
            {
                Console.WriteLine(test.Description);
                var result = extractor.Extract(test.Text).Select(x => test.Text.Substring(x.StartIndex, x.Length));
                if (!result.SequenceEqual(test.Expected))
                {
                    Debugger.Break();
                }
            }

            Console.WriteLine("End");
            Console.ReadLine();
        }

        private const string testFile = "extract.yml";

        static void DownloadTests()
        {
            Console.WriteLine("Downloading extract.yml");
            new WebClient().DownloadFile(
                "https://raw.githubusercontent.com/twitter/twitter-text/master/conformance/extract.yml",
                testFile);
        }

        static UrlsTest[] LoadTests()
        {
            if (!File.Exists(testFile))
                DownloadTests();

            TestYaml testYaml;

            using (var sr = new StreamReader(testFile))
            {
                var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
                testYaml = deserializer.Deserialize<TestYaml>(sr);
            }

            return testYaml.Tests.Urls.Concat(testYaml.Tests.UrlsWithIndices.Select(x => new UrlsTest
            {
                Description = x.Description,
                Text = x.Text,
                Expected = x.Expected.Select(y => y.Url).ToArray()
            })).ToArray();
        }
    }
}
