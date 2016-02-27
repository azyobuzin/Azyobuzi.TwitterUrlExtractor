using YamlDotNet.Serialization;

namespace Azyobuzi.TwitterUrlExtractor.Test
{
    class TestYaml
    {
        public ExtractorTests Tests { get; set; }
    }

    class ExtractorTests
    {
        public UrlsTest[] Urls { get; set; }
        [YamlMember(Alias = "urls_with_indices")]
        public UrlsWithIndicesTest[] UrlsWithIndices { get; set; }
    }

    class UrlsTest
    {
        public string Description { get; set; }
        public string Text { get; set; }
        public string[] Expected { get; set; }
    }

    class UrlsWithIndicesTest
    {
        public string Description { get; set; }
        public string Text { get; set; }
        public UrlsWithIndicesExpected[] Expected { get; set; }
    }

    class UrlsWithIndicesExpected
    {
        public string Url { get; set; }
        public int[] Indices { get; set; }
    }
}
