using System;
using System.Collections.Generic;
using System.Linq;

namespace Azyobuzi.TwitterUrlExtractor
{
    public sealed class Extractor
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
            public TldType Type;
            public int Length;

            public TldInfo(TldType type, int length)
            {
                this.Type = type;
                this.Length = length;
            }
        }

        private readonly Dictionary<int, TldInfo> _tldDictionary = new Dictionary<int, TldInfo>();
        private int _longestTldLength;
        private int _shortestTldLength;

        public Extractor(IEnumerable<string> gTlds, IEnumerable<string> ccTlds, IEnumerable<string> specialCcTlds)
        {
            if (gTlds != null)
                foreach (var x in gTlds) this.AddTld(x, TldType.GTld);

            var s = specialCcTlds != null ? specialCcTlds.ToArray() : new string[0];
            foreach (var x in s) this.AddTld(x, TldType.SpecialCcTld);

            if (ccTlds != null)
            {
                foreach (var x in ccTlds)
                {
                    if (Array.IndexOf(s, x) == -1)
                        this.AddTld(x, TldType.CcTld);
                }
            }
        }

        public Extractor() : this(DefaultTlds.GTlds, DefaultTlds.CTlds, DefaultTlds.SpecialCcTlds) { }

        private void AddTld(string tld, TldType type)
        {
            var len = tld.Length;
            if (len > this._longestTldLength)
                this._longestTldLength = len;
            else if (len < this._shortestTldLength)
                this._shortestTldLength = len;

            this._tldDictionary.Add(StringGetHashCode(tld), new TldInfo(type, len));
            // ハッシュが被ったら知らん
        }

        private static int ToLower(char c)
        {
            return c <= 'Z' && c >= 'A' ? (c + 32) : c;
        }

        private static int StringGetHashCode(string str)
        {
            var hash1 = 5381;
            var hash2 = hash1;

            for (var i = 0; i < str.Length;)
            {
                hash1 = ((hash1 << 5) + hash1) ^ ToLower(str[i++]);
                if (i >= str.Length) break;
                hash2 = ((hash2 << 5) + hash2) ^ ToLower(str[i++]);
            }

            return hash1 + hash2 * 1566083941;
        }

        [Flags]
        private enum CharType
        {
            None = 0,
            Alphabet = 1,
            Number = 1 << 1,
            At = 1 << 2,
            Alnum = Alphabet | Number,
            AlnumAt = Alnum | At,
            NotPrecedingSymbol = 1 << 3,
            NotPrecedingChar = Alnum | NotPrecedingSymbol,
            PathEndingSymbol = 1 << 4,
            PathSymbol = 1 << 5,
            QueryEndingSymbol = 1 << 6,
            QuerySymbol = 1 << 7,
            LParen = 1 << 8,
            RParen = 1 << 9,
            DomainSymbol = 1 << 10
        }

        private static readonly CharType[] AsciiTable =
        {
            CharType.None, // NUL
            CharType.None, // SOH
            CharType.None, // STX
            CharType.None, // ETX
            CharType.None, // EOX
            CharType.None, // ENQ
            CharType.None, // ACK
            CharType.None, // BEL
            CharType.None, // BS
            CharType.None, // HT
            CharType.None, // LF
            CharType.None, // VT
            CharType.None, // FF
            CharType.None, // CR
            CharType.None, // SO
            CharType.None, // SI
            CharType.None, // DLE
            CharType.None, // DC1
            CharType.None, // DC2
            CharType.None, // DC3
            CharType.None, // DC4
            CharType.None, // NAK
            CharType.None, // SYN
            CharType.None, // ETB
            CharType.None, // CAN
            CharType.None, // EM
            CharType.None, // SUB
            CharType.None, // ESC
            CharType.None, // FS
            CharType.None, // GS
            CharType.None, // RS
            CharType.None, // US
            CharType.None, // Space
            CharType.PathSymbol | CharType.QuerySymbol, // !
            CharType.None, // "
            CharType.NotPrecedingSymbol | CharType.PathEndingSymbol | CharType.QueryEndingSymbol, // #
            CharType.NotPrecedingSymbol | CharType.PathSymbol | CharType.QuerySymbol, // $
            CharType.PathSymbol | CharType.QuerySymbol, // %
            CharType.PathSymbol | CharType.QueryEndingSymbol, // &
            CharType.PathSymbol | CharType.QuerySymbol, // '
            CharType.QuerySymbol | CharType.LParen, // (
            CharType.QuerySymbol | CharType.RParen, // )
            CharType.PathSymbol | CharType.QuerySymbol, // *
            CharType.PathEndingSymbol | CharType.QuerySymbol, // +
            CharType.PathSymbol | CharType.QuerySymbol, // ,
            CharType.PathEndingSymbol | CharType.QuerySymbol | CharType.DomainSymbol, // -
            CharType.PathSymbol | CharType.QuerySymbol, // .
            CharType.PathEndingSymbol | CharType.QueryEndingSymbol, // /
            CharType.Number, // 0
            CharType.Number, // 1
            CharType.Number, // 2
            CharType.Number, // 3
            CharType.Number, // 4
            CharType.Number, // 5
            CharType.Number, // 6
            CharType.Number, // 7
            CharType.Number, // 8
            CharType.Number, // 9
            CharType.PathSymbol | CharType.QuerySymbol, // :
            CharType.PathSymbol | CharType.QuerySymbol, // ;
            CharType.None, // <
            CharType.PathEndingSymbol | CharType.QueryEndingSymbol, // =
            CharType.None, // >
            CharType.QuerySymbol, // ?
            CharType.At | CharType.NotPrecedingSymbol | CharType.PathSymbol | CharType.QuerySymbol, // @
            CharType.Alphabet, // A
            CharType.Alphabet, // B
            CharType.Alphabet, // C
            CharType.Alphabet, // D
            CharType.Alphabet, // E
            CharType.Alphabet, // F
            CharType.Alphabet, // G
            CharType.Alphabet, // H
            CharType.Alphabet, // I
            CharType.Alphabet, // J
            CharType.Alphabet, // K
            CharType.Alphabet, // L
            CharType.Alphabet, // M
            CharType.Alphabet, // N
            CharType.Alphabet, // O
            CharType.Alphabet, // P
            CharType.Alphabet, // Q
            CharType.Alphabet, // R
            CharType.Alphabet, // S
            CharType.Alphabet, // T
            CharType.Alphabet, // U
            CharType.Alphabet, // V
            CharType.Alphabet, // W
            CharType.Alphabet, // X
            CharType.Alphabet, // Y
            CharType.Alphabet, // Z
            CharType.PathSymbol | CharType.QuerySymbol, // [
            CharType.None, // \
            CharType.PathSymbol | CharType.QuerySymbol, // ]
            CharType.None, // ^
            CharType.PathEndingSymbol | CharType.QueryEndingSymbol | CharType.DomainSymbol, // _
            CharType.None, // `
            CharType.Alphabet, // a
            CharType.Alphabet, // b
            CharType.Alphabet, // c
            CharType.Alphabet, // d
            CharType.Alphabet, // e
            CharType.Alphabet, // f
            CharType.Alphabet, // g
            CharType.Alphabet, // h
            CharType.Alphabet, // i
            CharType.Alphabet, // j
            CharType.Alphabet, // k
            CharType.Alphabet, // l
            CharType.Alphabet, // m
            CharType.Alphabet, // n
            CharType.Alphabet, // o
            CharType.Alphabet, // p
            CharType.Alphabet, // q
            CharType.Alphabet, // r
            CharType.Alphabet, // s
            CharType.Alphabet, // t
            CharType.Alphabet, // u
            CharType.Alphabet, // v
            CharType.Alphabet, // w
            CharType.Alphabet, // x
            CharType.Alphabet, // y
            CharType.Alphabet, // z
            CharType.None, // {
            CharType.PathSymbol | CharType.QuerySymbol, // |
            CharType.None, // }
            CharType.PathSymbol | CharType.QuerySymbol, // ~
            CharType.None // DEL
        };

        private const int AsciiTableLength = 128;

        private static bool IsValidDomainChar(char c)
        {
            return c < AsciiTableLength
                ? (AsciiTable[c] & (CharType.Alnum | CharType.DomainSymbol)) != 0
                : !(
                    (c >= '\u2000' && c <= '\u206F') // General Punctuation
                    || c == '\u00A0' || c == '\u1680' || c == '\u3000' // Category 'Z'
                );
        }

        private static bool IsAccentChar(char c)
        {
            return (c >= '\u00c0' && c <= '\u00d6') || (c >= '\u00d8' && c <= '\u00f6') || (c >= '\u00f8' && c <= '\u024f')
                || c == '\u0253' || c == '\u0254' || c == '\u0256' || c == '\u0257' || c == '\u0259' || c == '\u025b' || c == '\u0263' || c == '\u0268' || c == '\u026f' || c == '\u0272' || c == '\u0289' || c == '\u028b'
                || c == '\u02bb'
                || (c >= '\u0300' && c <= '\u036f')
                || (c >= '\u1e00' && c <= '\u1eff');
        }

        private static bool IsUnicodeDomainChar(char c)
        {
            return c > 0x7F && !IsAccentChar(c);
        }

        private static bool IsAlnumAt(char c)
        {
            return c < AsciiTableLength && (AsciiTable[c] & CharType.AlnumAt) != 0;
        }

        private static bool IsAlnum(char c)
        {
            return c < AsciiTableLength && (AsciiTable[c] & CharType.Alnum) != 0;
        }

        private static bool IsPrecedingChar(char c)
        {
            return c < AsciiTableLength
                ? (AsciiTable[c] & CharType.NotPrecedingChar) == 0
                : !(c == '＠' || (c >= '\u202A' && c <= '\u202E'));
        }

        private static bool IsCyrillicScript(char c)
        {
            return (c >= '\u0400' && c <= '\u052F')
                || (c >= '\u2DE0' && c <= '\u2DFF') || (c >= '\uA640' && c <= '\uA69F')
                || c == '\u1D2B' || c == '\u1D78' || c == '\uFE2E' || c == '\uFE2F';
        }

        private static int EatPath(string text, int startIndex)
        {
            var lastEndingCharIndex = -1;
            var lastParenStartIndex = -1;
            var lastLengthInParen = 0;

            for (var i = startIndex; i < text.Length; i++)
            {
                var c = text[i];
                if (c < AsciiTableLength)
                {
                    switch (AsciiTable[c] & (CharType.Alnum | CharType.PathEndingSymbol | CharType.PathSymbol | CharType.LParen))
                    {
                        case 0:
                            goto BreakLoop;
                        case CharType.PathSymbol:
                            break;
                        case CharType.LParen:
                            lastLengthInParen = EatPathInParen(text, i + 1);
                            if (lastLengthInParen == 0)
                                goto BreakLoop;
                            lastParenStartIndex = i;
                            i += lastLengthInParen;
                            break;
                        default:
                            lastEndingCharIndex = i;
                            break;
                    }
                }
                else if (IsCyrillicScript(c) || IsAccentChar(c))
                {
                    lastEndingCharIndex = i;
                }
                else
                {
                    goto BreakLoop;
                }
            }

            BreakLoop:
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
                if (c < AsciiTableLength)
                {
                    switch (AsciiTable[c] & (CharType.Alnum | CharType.PathEndingSymbol | CharType.PathSymbol | CharType.LParen | CharType.RParen))
                    {
                        case 0:
                            goto BreakLoop;
                        case CharType.LParen:
                            var lengthInParen = EatPathInParen(text, i + 1);
                            if (lengthInParen == 0)
                                goto BreakLoop;
                            i += lengthInParen;
                            break;
                        case CharType.RParen:
                            return i - startIndex + 1;
                    }
                }
                else if (!IsCyrillicScript(c) && !IsAccentChar(c))
                {
                    goto BreakLoop;
                }
            }

            BreakLoop:
            return 0;
        }

        private static int EatQuery(string text, int startIndex)
        {
            var lastEndingCharIndex = -1;

            for (var i = startIndex; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= AsciiTableLength) break;
                switch (AsciiTable[c] & (CharType.Alnum | CharType.QueryEndingSymbol | CharType.QuerySymbol))
                {
                    case 0:
                        goto BreakLoop;
                    case CharType.QuerySymbol:
                        break;
                    default:
                        lastEndingCharIndex = i;
                        break;
                }
            }

            BreakLoop:
            return lastEndingCharIndex == -1 ? 0 : lastEndingCharIndex - startIndex + 1;
        }

        private void Extract(string text, List<EntityInfo> result)
        {
            var dots = new MiniList<int>();
            var hashCodes = new MiniList<int>();
            var startIndex = 0;

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
                        var j = i - 1;
                        if (text[j--] == '/' && text[j--] == ':')
                        {
                            switch (ToLower(text[j--]))
                            {
                                case 's':
                                    if (i >= 7 && ToLower(text[j--]) == 'p')
                                        goto case 'p';
                                    break;
                                case 'p':
                                    if (ToLower(text[j--]) == 't' && ToLower(text[j--]) == 't' && ToLower(text[j--]) == 'h')
                                    {
                                        if (j < 0 || IsPrecedingChar(text[j]))
                                        {
                                            precedingIndex = j;
                                            hasScheme = true;
                                            goto BreakSchemeCheck;
                                        }
                                    }
                                    break;
                            }
                        }
                    }

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

            BreakSchemeCheck:
            // ホスト部分を最後まで読み取る
            dots.Initialize();
            dots.Add(dotIndex + 1);
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

                    dots.Add(i + 1);
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
            TldInfo tldInfo;
            int dotCount;
            for (var i = dots.Count - 1; i >= 0; i--)
            {
                var dotIndexPlusOne = dots[i];
                var len = nextIndex - dotIndexPlusOne;
                if (len < this._shortestTldLength) continue;
                if (len > this._longestTldLength) len = this._longestTldLength;
                nextIndex = dotIndexPlusOne + len;

                // ループ回数軽減のため、その場でハッシュ値を求める
                hashCodes.Initialize();
                var hash1 = 5381;
                var hash2 = hash1;

                for (var j = dotIndexPlusOne; j < nextIndex;)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ ToLower(text[j++]);
                    hashCodes.Add(hash1 + hash2 * 1566083941);
                    if (j >= nextIndex) break;
                    hash2 = ((hash2 << 5) + hash2) ^ ToLower(text[j++]);
                    hashCodes.Add(hash1 + hash2 * 1566083941);
                }

                for (var j = hashCodes.Count - 1; j >= 0; j--)
                {
                    nextIndex = dotIndexPlusOne + j + 1;
                    if ((nextIndex == text.Length || !IsAlnumAt(text[nextIndex]))
                        && this._tldDictionary.TryGetValue(hashCodes[j], out tldInfo)
                        && nextIndex - dotIndexPlusOne == tldInfo.Length) // ハッシュ衝突の簡易チェック
                    {
                        dotCount = i + 1;
                        goto TldDecided;
                    }
                }
            }

            goto GoToNextToDot;

            TldDecided:
            // ccTLD のサブドメインなしはスキーム必須
            if (!hasScheme && tldInfo.Type == TldType.CcTld
                && (dotCount == 1 && (nextIndex >= text.Length || text[nextIndex] != '/')))
                goto GoToNextIndex;

            // サブドメインには _ を使えるがドメインには使えない
            for (var i = dots.Last - 2; i > precedingIndex; i--)
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
                    var c = text[nextIndex];
                    if (c <= '9' && c >= '0')
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
                // https?://t.co/xxxxxxxxxx だけ特別扱い
                var len = nextIndex - urlStartIndex;
                nextIndex++;
                if (hasScheme && (len == 11 || len == 12)
                    && ToLower(text[nextIndex - 2]) == 'o' && ToLower(text[nextIndex - 3]) == 'c'
                    && text[nextIndex - 4] == '.' && ToLower(text[nextIndex - 5]) == 't' && text[nextIndex - 6] == '/'
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
            var result = new List<EntityInfo>();
            if (!string.IsNullOrEmpty(text))
                this.Extract(text, result);
            return result;
        }
    }
}
