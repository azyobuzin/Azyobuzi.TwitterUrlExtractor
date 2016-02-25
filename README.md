# Azyobuzi.TwitterUrlExtractor
Fast URL extractor for Twitter clients.

22x faster on average than [twitter-text-cs](https://github.com/niltz/twitter-text-cs).

# How to use
```csharp
using Azyobuzi.TwitterUrlExtractor;

var text = "twitter.comこれは日本語です。example.com中国語t.co/abcde한국twitter.com example2.comテストtwitter.com/abcde";

var extractor = new Extractor();
List<EntityInfo> result = extractor.Extract(text);

foreach (var x in result)
{
    Console.WriteLine(text.Substring(x.StartIndex, x.Length));
}

/*
twitter.com
example.com
t.co/abcde
twitter.com
example2.com
twitter.com/abcde
*/
```

# Install
```
PM> Install-Package Azyobuzi.TwitterUrlExtractor
```
