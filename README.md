# Azyobuzi.TwitterUrlExtractor
The fastest URL extractor (I think) for Twitter clients.

108x faster than [twitter-text-cs](https://github.com/niltz/twitter-text-cs).

# Install
```
PM> Install-Package Azyobuzi.TwitterUrlExtractor
```

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

Note that the constructor of Extractor is little heavy, so you should reuse one instance.

## Count the tweet length
```csharp
var text = "http://www.bestbuy.com/site/Currie+Technologies+-+Ezip+400+Scooter/9885188.p?id=1218189013070&skuId=9885188";
text = text.Normalize();

var extractor = new Extractor();
var result = extractor.Extract(text);

var length = text.Count(x => !char.IsLowSurrogate(x))
    - result.Sum(x => x.Length) + 23 * result.Count;

// 23
```
