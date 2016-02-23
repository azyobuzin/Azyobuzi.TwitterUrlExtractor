using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Azyobuzi.TwitterUrlExtractor
{
    public class Extractor
    {
        private enum TldType
        {
            None,
            GTld,
            CcTld,
            SpecialCcTld
        }

        private struct TldInfo
        {
            public string Value;
            public TldType Type;

            public TldInfo(string value, TldType type)
            {
                this.Value = value;
                this.Type = type;
            }
        }

        private class TldInfoComparer : IComparer<TldInfo>, IEqualityComparer<TldInfo>
        {
            public int Compare(TldInfo x, TldInfo y)
            {
                var xlen = x.Value.Length;
                var ylen = y.Value.Length;

                for (var i = 0; i < Math.Min(xlen, ylen); i++)
                {
                    var c = x.Value[i].CompareTo(y.Value[i]);
                    if (c != 0) return c;
                }

                // 長い方が先に来るように
                return xlen > ylen
                    ? -1
                    : xlen < ylen
                        ? 1
                        : 0;
            }

            public bool Equals(TldInfo x, TldInfo y)
            {
                return x.Value.Equals(y.Value, StringComparison.Ordinal);
            }

            public int GetHashCode(TldInfo obj)
            {
                return obj.Value.GetHashCode();
            }
        }

        private readonly Dictionary<char, ArraySegment<TldInfo>> _tldFirstCharDic = new Dictionary<char, ArraySegment<TldInfo>>();
        private int _longestTldLength;
        private int _shortestTldLength;

        /// <param name="gTlds">gTLDのリスト。すべて小文字である必要があります（チェックされません）。</param>
        /// <param name="ccTlds">ccTLDのリスト。すべて小文字である必要があります（チェックされません）。</param>
        /// <param name="specialCcTlds">gTLDのように扱うccTLDのリスト。すべて小文字である必要があります（チェックされません）。</param>
        public Extractor(IEnumerable<string> gTlds, IEnumerable<string> ccTlds, IEnumerable<string> specialCcTlds)
        {
            // ccTlds より specialCcTlds が優先されるように
            var comparer = new TldInfoComparer();
            var tlds = specialCcTlds.Select(x => new TldInfo(x, TldType.SpecialCcTld))
                .Union(ccTlds.Select(x => new TldInfo(x, TldType.CcTld)), comparer)
                .Concat(gTlds.Select(x => new TldInfo(x, TldType.GTld)));
            var tldList = new List<TldInfo>();
            foreach (var x in tlds)
            {
                if (x.Value.Length > this._longestTldLength)
                    this._longestTldLength = x.Value.Length;
                else if (x.Value.Length < this._shortestTldLength)
                    this._shortestTldLength = x.Value.Length;

                tldList.Add(x);
            }

            var tldArray = tldList.ToArray();
            Array.Sort(tldArray, comparer);

            var startIndex = 0;
            var firstChar = default(char);
            while (startIndex < tldArray.Length)
            {
                firstChar = tldArray[startIndex].Value[0];
                var i = startIndex + 1;
                for (; i < tldArray.Length && tldArray[i].Value[0] == firstChar; i++) { }
                this._tldFirstCharDic.Add(firstChar, new ArraySegment<TldInfo>(tldArray, startIndex, i - startIndex));
                startIndex = i;
            }
        }

        public Extractor() : this(DefaultTlds.GTlds, DefaultTlds.CTlds, DefaultTlds.SpecialCcTlds) { }

        private static char ToLower(char c)
        {
            return c >= 'A' && c <= 'Z'
                ? (char)(c + 32) // - 'A' + 'a' ってやってもコンパイル時に展開してくれないんですがこれは…
                : c;
        }

        private static bool IsValidDomainChar(char c)
        {
            if (c == '-' || c == '_')
                return true;

            if ((c >= '!' && c <= '/') || (c >= ':' && c <= '@') || (c >= '[' && c <= '`') || (c >= '{' && c <= '~'))
                return false;

            switch (CharUnicodeInfo.GetUnicodeCategory(c))
            {
                case UnicodeCategory.Control:
                // Punct
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.OtherPunctuation:
                // Separator(Z)
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.SpaceSeparator:
                    return false;
                default:
                    return true;
            }
        }

        private static bool IsAccentChar(char c)
        {
            return (c >= '\u00c0' && c <= '\u00d6') || (c >= '\u00d8' && c <= '\u00f6') || (c >= '\u00f8' && c <= '\u00ff')
                || (c >= '\u0100' && c <= '\u024f')
                || c == '\u0253' || c == '\u0254' || c == '\u0256' || c == '\u0257' || c == '\u0259' || c == '\u025b' || c == '\u0263' || c == '\u0268' || c == '\u026f' || c == '\u0272' || c == '\u0289' || c == '\u028b'
                || c == '\u02bb'
                || (c >= '\u0300' && c <= '\u036f')
                || (c >= '\u1e00' && c <= '\u1eff');
        }

        private static bool IsUnicodeDomainChar(char c)
        {
            return c > 0x7F && !IsAccentChar(c);
        }

        private static bool IsNum(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool IsAlnumAt(char c)
        {
            return (c >= '@' && c <= 'Z') || (c >= 'a' && c <= 'z') || IsNum(c);
        }

        private static bool IsAlnum(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || IsNum(c);
        }

        private static bool IsPrecedingChar(char c)
        {
            var b = c == '＠' || c == '$' || c == '#' || c == '＃'
                || IsAlnumAt(c) || (c >= '\u202A' && c <= '\u202E');
            return !b;
        }

        private static bool IsCyrillicScript(char c)
        {
            return (c >= '\u0400' && c <= '\u04FF') || (c >= '\u0500' && c <= '\u052F')
                || (c >= '\u2DE0' && c <= '\u2DFF') || (c >= '\uA640' && c <= '\uA69F')
                || c == '\u1D2B' || c == '\u1D78' || c == '\uFE2E' || c == '\uFE2F';
        }

        private static bool IsPathEndingChar(char c)
        {
            return IsAlnum(c) || c == '=' || c == '_' || c == '#' || c == '/' || c == '-' || c == '+' || c == '"'
                || IsCyrillicScript(c) || IsAccentChar(c);
        }

        private static bool IsPathChar(char c)
        {
            return c == '!' || (c >= '#' && c <= '\'') || (c >= '*' && c <= ';')
               || c == '=' || (c >= '@' && c <= '[') || (c >= 'a' && c <= 'z')
               || c == ']' || c == '_' && c == '|' || c == '~'
               || IsCyrillicScript(c) || IsAccentChar(c);
        }

        private static int EatPath(string text, int startIndex)
        {
            var lastEndingCharIndex = -1;
            var lastParenStartIndex = -1;
            var lastLengthInParen = 0;

            for (var i = startIndex; i < text.Length; i++)
            {
                var c = text[i];

                if (IsPathEndingChar(c))
                {
                    lastEndingCharIndex = i;
                }
                else if (!IsPathChar(c))
                {
                    if (c == '(')
                    {
                        lastLengthInParen = EatPathInParen(text, i + 1);
                        if (lastLengthInParen == 0)
                            break;
                        lastParenStartIndex = i;
                        i += lastLengthInParen;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if ((lastEndingCharIndex == -1 && lastParenStartIndex == startIndex) // 「a.com/(a)」などに対応
                || (lastEndingCharIndex + 1 == lastParenStartIndex)) // 「twitter.com/test(a).」は「twitter.com/test(a)」まで
            {
                lastEndingCharIndex = lastParenStartIndex + lastLengthInParen;
            }

            return lastEndingCharIndex == -1 ? 0 : lastEndingCharIndex - startIndex + 1;
        }

        private static int EatPathInParen(string text, int startIndex)
        {
            for (var i = startIndex; i < text.Length; i++)
            {
                var c = text[i];
                if (!IsPathChar(c))
                {
                    if (c == '(')
                    {
                        var lengthInParen = EatPathInParen(text, i + 1);
                        if (lengthInParen == 0)
                            break;
                        i += lengthInParen;
                    }
                    else if (c == ')')
                    {
                        return i - startIndex + 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return 0;
        }

        private static bool IsQueryEndingChar(char c)
        {
            return (c >= '/' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
                || c == '_' || c == '&' || c == '=' || c == '#';
        }

        private static bool IsQueryCharWithoutEnding(char c)
        {
            return c == '!' || (c >= '#' && c <= ';') || c == '=' || (c >= '?' && c <= '[')
                || c == ']' || c == '|' || c == '~';
        }

        private static int EatQuery(string text, int startIndex)
        {
            var lastEndingCharIndex = -1;

            for (var i = startIndex; i < text.Length; i++)
            {
                var c = text[i];

                if (IsQueryEndingChar(c))
                {
                    lastEndingCharIndex = i;
                }
                else if (!IsQueryCharWithoutEnding(c))
                {
                    break;
                }
            }

            return lastEndingCharIndex == -1 ? 0 : lastEndingCharIndex - startIndex + 1;
        }

        private struct DotSplitInfo
        {
            public int DotIndexPlusOne;
            public bool HasAnyUnicodeCharsBetweenFirstDotAndThis;

            public DotSplitInfo(int dotIndex, bool hasAnyUnicodeCharsBetweenFirstDotAndThis)
            {
                this.DotIndexPlusOne = dotIndex + 1;
                this.HasAnyUnicodeCharsBetweenFirstDotAndThis = hasAnyUnicodeCharsBetweenFirstDotAndThis;
            }
        }

        private static bool TryGetTld(Dictionary<char, ArraySegment<TldInfo>> dic, string text, int index, int maxLen, out TldInfo tld)
        {
            ArraySegment<TldInfo> seg;
            if (dic.TryGetValue(ToLower(text[index]), out seg))
            {
                var segEnd = seg.Offset + seg.Count;
                for (var i = seg.Offset; i < segEnd; i++)
                {
                    var info = seg.Array[i];
                    var tldLen = info.Value.Length;
                    var nextIndex = index + tldLen;

                    // 次の文字が英数@ならばダメ
                    if (tldLen > maxLen || (text.Length != nextIndex && IsAlnumAt(text[nextIndex])))
                        continue;

                    for (var j = 1; j < tldLen; j++)
                    {
                        if (ToLower(text[index + j]) != info.Value[j])
                            goto NextTld;
                    }

                    tld = info;
                    return true;

                    NextTld:
                    continue;
                }
            }
            tld = default(TldInfo);
            return false;
        }

        private void Extract(string text, int startIndex, List<EntityInfo> result)
        {
            var dots = new MiniList<DotSplitInfo>();

            Start:
            if (startIndex >= text.Length - 2) return;

            var dotIndex = text.IndexOf('.', startIndex);
            if (dotIndex == -1 || dotIndex == text.Length - 1) return;
            if (dotIndex == startIndex)
            {
                // 開始位置にいきなり . があったら正しい URL なわけないでしょ
                goto GoToNextToDot;
            }

            // dotIndex の位置
            // www.(←)twitter.com/
            // twitter.(←)com/

            // . の前が - や _ なら終了
            var x = text[dotIndex - 1];
            if (x == '-' || x == '_')
                goto GoToNextToDot;

            // 前方向に探索
            // PrecedingChar まで戻る
            var precedingIndex = -1;
            var lastUnicodeCharIndex = -1;
            var hasScheme = false;
            for (var i = dotIndex - 1; i >= startIndex; i--)
            {
                var c = text[i];

                if (c == '/')
                {
                    // ホストの最初が - や _ なら終了
                    x = text[i + 1];
                    if (x == '-' || x == '_')
                        goto GoToNextToDot;

                    // スキーム判定
                    if (i >= 6)
                    {
                        precedingIndex = i - 7;
                        hasScheme = text.Substring(i - 6, 6).Equals("http:/", StringComparison.OrdinalIgnoreCase)
                            && (i == 6 || IsPrecedingChar(text[precedingIndex]));
                    }
                    if (!hasScheme && i >= 7)
                    {
                        precedingIndex = i - 8;
                        hasScheme = text.Substring(i - 7, 7).Equals("https:/", StringComparison.OrdinalIgnoreCase)
                            && (i == 7 || IsPrecedingChar(text[precedingIndex]));
                    }

                    if (hasScheme) break;

                    goto GoToNextToDot;
                }

                if (!IsValidDomainChar(c))
                {
                    if (IsPrecedingChar(c))
                    {
                        precedingIndex = i;
                        break;
                    }

                    // PrecedingChar でないなら無効
                    goto GoToNextToDot;
                }

                if (lastUnicodeCharIndex == -1 && IsUnicodeDomainChar(c))
                    lastUnicodeCharIndex = i;
            }

            if (!hasScheme && lastUnicodeCharIndex != -1)
            {
                if (lastUnicodeCharIndex != dotIndex - 1 && IsPrecedingChar(text[lastUnicodeCharIndex]))
                {
                    // Unicode文字を含まないようにして救済
                    precedingIndex = lastUnicodeCharIndex;
                    lastUnicodeCharIndex = -1;
                }
                else
                {
                    goto GoToNextToDot;
                }
            }

            x = text[precedingIndex + 1];
            if ((precedingIndex == -1 && startIndex != 0) || x == '-' || x == '_')
                goto GoToNextToDot;

            // ホスト部分を最後まで読み取る
            dots.Initialize();
            dots.Add(new DotSplitInfo(dotIndex, false));
            var hasUnicodeCharAfterDot = false;
            var nextIndex = text.Length;
            for (var i = dotIndex + 1; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '.')
                {
                    // . が text の最後なら終了
                    // スキームなしなのに Unicode 文字が含まれていたら終了
                    if (i == text.Length - 1 || (!hasScheme && hasUnicodeCharAfterDot))
                    {
                        nextIndex = i;
                        break;
                    }

                    // . の前後の文字が - や _ なら終了
                    x = text[i - 1];
                    if (x == '-' || x == '_')
                    {
                        nextIndex = i - 1;
                        break;
                    }
                    x = text[i + 1];
                    if (x == '-' || x == '_')
                    {
                        nextIndex = i;
                        break;
                    }

                    dots.Add(new DotSplitInfo(i, hasUnicodeCharAfterDot));
                    continue;
                }

                if (!IsValidDomainChar(c))
                {
                    nextIndex = i;
                    break;
                }

                if (!hasUnicodeCharAfterDot)
                    hasUnicodeCharAfterDot = IsUnicodeDomainChar(c);
            }

            // TLD 検証
            TldInfo tld;
            int dotCount;
            for (var i = dots.Count - 1; i >= 0; i--)
            {
                var s = dots[i];
                var len = nextIndex - s.DotIndexPlusOne;
                if (len < this._shortestTldLength) continue;
                if (len > this._longestTldLength) len = this._longestTldLength;

                if (TryGetTld(this._tldFirstCharDic, text, s.DotIndexPlusOne, len, out tld))
                {
                    dotCount = i + 1;
                    nextIndex = s.DotIndexPlusOne + tld.Value.Length;
                    goto TldDecided;
                }

            }

            goto GoToNextToDot;

            TldDecided:
            // ccTLD のサブドメインなしはスキーム必須
            if (!hasScheme && tld.Type == TldType.CcTld
                && (dotCount == 1 && (nextIndex >= text.Length || text[nextIndex] != '/')))
                goto GoToNextIndex;

            // サブドメインには _ を使えるがドメインには使えない
            for (var i = dots.Last.DotIndexPlusOne - 2; i > precedingIndex; i--)
            {
                var c = text[i];
                if (c == '.' || c == '/') break;
                if (c == '_')
                    goto GoToNextIndex;
            }

            var urlStartIndex = precedingIndex + 1;

            if (nextIndex >= text.Length)
                goto AddAndGoNext;

            // ポート番号
            if (text[nextIndex] == ':' && ++nextIndex < text.Length)
            {
                var portNumberLength = 0;
                for (; nextIndex < text.Length; nextIndex++)
                {
                    if (IsNum(text[nextIndex]))
                        portNumberLength++;
                    else
                        break;
                }

                if (portNumberLength == 0)
                {
                    result.Add(new EntityInfo(urlStartIndex, (--nextIndex) - urlStartIndex));
                    goto GoToNextIndex;
                }
            }

            if (nextIndex >= text.Length)
                goto AddAndGoNext;

            // パス
            if (text[nextIndex] == '/')
            {
                nextIndex++;

                // https?://t.co/xxxxxxxxxx だけ特別扱い
                var strBeforePath = text.Substring(urlStartIndex, nextIndex - 1 - urlStartIndex);
                if ((strBeforePath.Equals("https://t.co", StringComparison.OrdinalIgnoreCase)
                    || strBeforePath.Equals("http://t.co", StringComparison.OrdinalIgnoreCase))
                    && nextIndex < text.Length && IsAlnum(text[nextIndex]))
                {
                    nextIndex++;
                    for (; nextIndex < text.Length; nextIndex++)
                    {
                        if (!IsAlnum(text[nextIndex]))
                            break;
                    }
                    goto AddAndGoNext;
                }

                nextIndex += EatPath(text, nextIndex);
            }

            if (nextIndex >= text.Length)
                goto AddAndGoNext;

            // クエリ
            if (text[nextIndex] == '?')
            {
                nextIndex++;
                nextIndex += EatQuery(text, nextIndex);
            }

            AddAndGoNext:
            result.Add(new EntityInfo(urlStartIndex, nextIndex - urlStartIndex));

            GoToNextIndex:
            startIndex = nextIndex;
            goto Start;

            GoToNextToDot:
            startIndex = dotIndex + 1;
            goto Start;
        }

        public List<EntityInfo> Extract(string text)
        {
            var result = new List<EntityInfo>(4);
            if (!string.IsNullOrEmpty(text))
                this.Extract(text, 0, result);
            return result;
        }
    }
}
