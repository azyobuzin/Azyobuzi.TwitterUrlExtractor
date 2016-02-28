package net.azyobuzi.twitterurlextractor;

import java.text.Normalizer;
import java.util.*;

public class Extractor {
    private static final int GTLD = 0;
    private static final int CCTLD = 1 << 31;
    private static final int SPECIAL_CCTLD = 1 << 30;

    private static final int CHAR_ALPHABET = 1;
    private static final int CHAR_NUMBER = 1 << 1;
    private static final int CHAR_AT = 1 << 2;
    private static final int CHAR_NOT_PRECEDING_SYMBOL = 1 << 3;
    private static final int CHAR_PATH_ENDING_SYMBOL = 1 << 4;
    private static final int CHAR_PATH_SYMBOL = 1 << 5;
    private static final int CHAR_QUERY_ENDING_SYMBOL = 1 << 6;
    private static final int CHAR_QUERY_SYMBOL = 1 << 7;
    private static final int CHAR_LPAREN = 1 << 8;
    private static final int CHAR_RPAREN = 1 << 9;
    private static final int CHAR_DOMAIN_SYMBOL = 1 << 10;

    private static final int[] ASCII_TABLE =
            {
                    0, // NUL
                    0, // SOH
                    0, // STX
                    0, // ETX
                    0, // EOX
                    0, // ENQ
                    0, // ACK
                    0, // BEL
                    0, // BS
                    0, // HT
                    0, // LF
                    0, // VT
                    0, // FF
                    0, // CR
                    0, // SO
                    0, // SI
                    0, // DLE
                    0, // DC1
                    0, // DC2
                    0, // DC3
                    0, // DC4
                    0, // NAK
                    0, // SYN
                    0, // ETB
                    0, // CAN
                    0, // EM
                    0, // SUB
                    0, // ESC
                    0, // FS
                    0, // GS
                    0, // RS
                    0, // US
                    0, // Space
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // !
                    0, // "
                    CHAR_NOT_PRECEDING_SYMBOL | CHAR_PATH_ENDING_SYMBOL | CHAR_QUERY_ENDING_SYMBOL, // #
                    CHAR_NOT_PRECEDING_SYMBOL | CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // $
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // %
                    CHAR_PATH_SYMBOL | CHAR_QUERY_ENDING_SYMBOL, // &
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // '
                    CHAR_QUERY_SYMBOL | CHAR_LPAREN, // (
                    CHAR_QUERY_SYMBOL | CHAR_RPAREN, // )
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // *
                    CHAR_PATH_ENDING_SYMBOL | CHAR_QUERY_SYMBOL, // +
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // ,
                    CHAR_PATH_ENDING_SYMBOL | CHAR_QUERY_SYMBOL | CHAR_DOMAIN_SYMBOL, // -
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // .
                    CHAR_PATH_ENDING_SYMBOL | CHAR_QUERY_ENDING_SYMBOL, // /
                    CHAR_NUMBER, // 0
                    CHAR_NUMBER, // 1
                    CHAR_NUMBER, // 2
                    CHAR_NUMBER, // 3
                    CHAR_NUMBER, // 4
                    CHAR_NUMBER, // 5
                    CHAR_NUMBER, // 6
                    CHAR_NUMBER, // 7
                    CHAR_NUMBER, // 8
                    CHAR_NUMBER, // 9
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // :
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // ;
                    0, // <
                    CHAR_PATH_ENDING_SYMBOL | CHAR_QUERY_ENDING_SYMBOL, // =
                    0, // >
                    CHAR_QUERY_SYMBOL, // ?
                    CHAR_AT | CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // @
                    CHAR_ALPHABET, // A
                    CHAR_ALPHABET, // B
                    CHAR_ALPHABET, // C
                    CHAR_ALPHABET, // D
                    CHAR_ALPHABET, // E
                    CHAR_ALPHABET, // F
                    CHAR_ALPHABET, // G
                    CHAR_ALPHABET, // H
                    CHAR_ALPHABET, // I
                    CHAR_ALPHABET, // J
                    CHAR_ALPHABET, // K
                    CHAR_ALPHABET, // L
                    CHAR_ALPHABET, // M
                    CHAR_ALPHABET, // N
                    CHAR_ALPHABET, // O
                    CHAR_ALPHABET, // P
                    CHAR_ALPHABET, // Q
                    CHAR_ALPHABET, // R
                    CHAR_ALPHABET, // S
                    CHAR_ALPHABET, // T
                    CHAR_ALPHABET, // U
                    CHAR_ALPHABET, // V
                    CHAR_ALPHABET, // W
                    CHAR_ALPHABET, // X
                    CHAR_ALPHABET, // Y
                    CHAR_ALPHABET, // Z
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // [
                    0, // \
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // ]
                    0, // ^
                    CHAR_PATH_ENDING_SYMBOL | CHAR_QUERY_ENDING_SYMBOL | CHAR_DOMAIN_SYMBOL, // _
                    0, // `
                    CHAR_ALPHABET, // a
                    CHAR_ALPHABET, // b
                    CHAR_ALPHABET, // c
                    CHAR_ALPHABET, // d
                    CHAR_ALPHABET, // e
                    CHAR_ALPHABET, // f
                    CHAR_ALPHABET, // g
                    CHAR_ALPHABET, // h
                    CHAR_ALPHABET, // i
                    CHAR_ALPHABET, // j
                    CHAR_ALPHABET, // k
                    CHAR_ALPHABET, // l
                    CHAR_ALPHABET, // m
                    CHAR_ALPHABET, // n
                    CHAR_ALPHABET, // o
                    CHAR_ALPHABET, // p
                    CHAR_ALPHABET, // q
                    CHAR_ALPHABET, // r
                    CHAR_ALPHABET, // s
                    CHAR_ALPHABET, // t
                    CHAR_ALPHABET, // u
                    CHAR_ALPHABET, // v
                    CHAR_ALPHABET, // w
                    CHAR_ALPHABET, // x
                    CHAR_ALPHABET, // y
                    CHAR_ALPHABET, // z
                    0, // {
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // |
                    0, // }
                    CHAR_PATH_SYMBOL | CHAR_QUERY_SYMBOL, // ~
                    0 // DEL
            };

    private static final int ASCII_TABLE_LENGTH = 128;

    private final IntIntMap4a tldMap;
    private int longestTldLength;
    private int shortestTldLength = Integer.MAX_VALUE;

    public Extractor(List<String> gTlds, List<String> ccTlds, List<String> specialCcTlds) {
        int size = 0;
        if (gTlds != null) size += gTlds.size();
        if (ccTlds != null) size += ccTlds.size();
        if (specialCcTlds != null) size += specialCcTlds.size();
        tldMap = new IntIntMap4a(size, 0.75f);

        if (gTlds != null) {
            for (String x : gTlds)
                this.addTld(x, GTLD);
        }

        if (specialCcTlds != null) {
            for (String x : specialCcTlds)
                this.addTld(x, SPECIAL_CCTLD);
        } else {
            specialCcTlds = new ArrayList<>();
        }

        if (ccTlds != null) {
            L:
            for (String x : ccTlds) {
                for (String y : specialCcTlds) {
                    if (x.equalsIgnoreCase(y)) continue L;
                }

                this.addTld(x, CCTLD);
            }
        }
    }

    public Extractor(List<String> gTlds, List<String> ccTlds) {
        this(gTlds, ccTlds, Arrays.asList("co", "tv"));
    }

    private static int toLower(char c) {
        return c <= 'Z' && c >= 'A' ? (c + 32) : c;
    }

    private static int getHashCode(CharSequence str) {
        int hash1 = 5381;
        int hash2 = hash1;
        int len = str.length();

        for (int i = 0; i < len; ) {
            hash1 = ((hash1 << 5) + hash1) ^ toLower(str.charAt(i++));
            if (i >= len) break;
            hash2 = ((hash2 << 5) + hash2) ^ toLower(str.charAt(i++));
        }

        return hash1 + hash2 * 1566083941;
    }

    private void addTld(CharSequence tld, int type) {
        int len = tld.length();

        // フラグを持たせるスペースを確保
        if ((len & 0b11 << 30) != 0)
            throw new IllegalArgumentException("Too long TLD: " + tld);

        if (len > this.longestTldLength)
            this.longestTldLength = len;
        else if (len < this.shortestTldLength)
            this.shortestTldLength = len;

        this.tldMap.put(getHashCode(tld), type | len);
    }

    private static boolean isValidDomainChar(char c) {
        return c < ASCII_TABLE_LENGTH
                ? (ASCII_TABLE[c] & (CHAR_ALPHABET | CHAR_NUMBER | CHAR_DOMAIN_SYMBOL)) != 0
                : !((c >= '\u2000' && c <= '\u206F') || c == '\u00A0' || c == '\u1680' || c == '\u3000');
    }

    private static boolean isAccentChar(char c) {
        return (c >= '\u00c0' && c <= '\u00d6') || (c >= '\u00d8' && c <= '\u00f6') || (c >= '\u00f8' && c <= '\u024f')
                || c == '\u0253' || c == '\u0254' || c == '\u0256' || c == '\u0257' || c == '\u0259' || c == '\u025b' || c == '\u0263' || c == '\u0268' || c == '\u026f' || c == '\u0272' || c == '\u0289' || c == '\u028b'
                || c == '\u02bb'
                || (c >= '\u0300' && c <= '\u036f')
                || (c >= '\u1e00' && c <= '\u1eff');
    }

    private static boolean isUnicodeDomainChar(char c) {
        return c > 0x7F && !isAccentChar(c);
    }

    private static boolean isAlnumAt(char c) {
        return c < ASCII_TABLE_LENGTH && (ASCII_TABLE[c] & (CHAR_ALPHABET | CHAR_NUMBER | CHAR_AT)) != 0;
    }

    private static boolean isAlnum(char c) {
        return c < ASCII_TABLE_LENGTH && (ASCII_TABLE[c] & (CHAR_ALPHABET | CHAR_NUMBER)) != 0;
    }

    private static boolean isPrecedingChar(char c) {
        return c < ASCII_TABLE_LENGTH
                ? (ASCII_TABLE[c] & (CHAR_ALPHABET | CHAR_NUMBER | CHAR_NOT_PRECEDING_SYMBOL)) == 0
                : !(c == '＠' || (c >= '\u202A' && c <= '\u202E'));
    }

    private static boolean isCyrillicScript(char c) {
        return (c >= '\u0400' && c <= '\u052F')
                || (c >= '\u2DE0' && c <= '\u2DFF') || (c >= '\uA640' && c <= '\uA69F')
                || c == '\u1D2B' || c == '\u1D78' || c == '\uFE2E' || c == '\uFE2F';
    }

    private static int eatPath(String text, int startIndex) {
        int lastEndingCharIndex = -1;
        int lastParenStartIndex = -1;
        int lastLengthInParen = 0;

        L:
        for (int i = startIndex; i < text.length(); i++) {
            char c = text.charAt(i);
            if (c < ASCII_TABLE_LENGTH) {
                switch (ASCII_TABLE[c] & (CHAR_ALPHABET | CHAR_NUMBER | CHAR_PATH_ENDING_SYMBOL | CHAR_PATH_SYMBOL | CHAR_LPAREN)) {
                    case 0:
                        break L;
                    case CHAR_PATH_SYMBOL:
                        break;
                    case CHAR_LPAREN:
                        lastLengthInParen = eatPathInParen(text, i + 1);
                        if (lastLengthInParen == 0) break L;
                        lastParenStartIndex = i;
                        i += lastLengthInParen;
                        break;
                    default:
                        lastEndingCharIndex = i;
                        break;
                }
            } else if (isCyrillicScript(c) || isAccentChar(c)) {
                lastEndingCharIndex = i;
            } else {
                break L;
            }
        }

        if ((lastEndingCharIndex == -1 && lastParenStartIndex == startIndex) // 「a.com/(a)」などに対応
                || (lastEndingCharIndex + 1 == lastParenStartIndex)) // 「twitter.com/test(a).」は「twitter.com/test(a)」まで
        {
            lastEndingCharIndex = lastParenStartIndex + lastLengthInParen;
        }

        return lastEndingCharIndex == -1 ? 0 : lastEndingCharIndex - startIndex + 1;
    }

    private static int eatPathInParen(String text, int startIndex) {
        L:
        for (int i = startIndex; i < text.length(); i++) {
            char c = text.charAt(i);
            if (c < ASCII_TABLE_LENGTH) {
                switch (ASCII_TABLE[c] & (CHAR_ALPHABET | CHAR_NUMBER | CHAR_PATH_ENDING_SYMBOL | CHAR_PATH_SYMBOL | CHAR_LPAREN | CHAR_RPAREN)) {
                    case 0:
                        break L;
                    case CHAR_LPAREN:
                        int lengthInParen = eatPathInParen(text, i + 1);
                        if (lengthInParen == 0) break L;
                        i += lengthInParen;
                        break;
                    case CHAR_RPAREN:
                        return i - startIndex + 1;
                }
            } else if (!isCyrillicScript(c) && !isAccentChar(c)) {
                break L;
            }
        }

        return 0;
    }

    private static int eatQuery(String text, int startIndex) {
        int lastEndingCharIndex = -1;

        L:
        for (int i = startIndex; i < text.length(); i++) {
            char c = text.charAt(i);
            if (c >= ASCII_TABLE_LENGTH) break;
            switch (ASCII_TABLE[c] & (CHAR_ALPHABET | CHAR_NUMBER | CHAR_QUERY_ENDING_SYMBOL | CHAR_QUERY_SYMBOL)) {
                case 0:
                    break L;
                case CHAR_QUERY_SYMBOL:
                    break;
                default:
                    lastEndingCharIndex = i;
                    break;
            }
        }

        return lastEndingCharIndex == -1 ? 0 : lastEndingCharIndex - startIndex + 1;
    }

    private void extract(String text, List<EntityInfo> result) {
        IntList dots = new IntList();
        IntList hashCodes = new IntList();
        int startIndex = 0;

        Start:
        while (true) {
            if (startIndex >= text.length() - 2) return;

            int dotIndex = text.indexOf('.', startIndex);

            GoToNextToDot:
            {
                if (dotIndex == -1 || dotIndex == text.length() - 1) return;
                if (dotIndex == startIndex) {
                    break GoToNextToDot;
                }

                char x = text.charAt(dotIndex - 1);
                if (x == '-' || x == '_')
                    break GoToNextToDot;

                int precedingIndex = -1;
                int lastUnicodeCharIndex = -1;
                boolean hasScheme = false;
                SchemeCheck:
                {
                    for (int i = dotIndex - 1; i >= startIndex; i--) {
                        char c = text.charAt(i);

                        if (c == '/') {
                            // ホストの最初が - や _ なら終了
                            x = text.charAt(i + 1);
                            if (x == '-' || x == '_')
                                break GoToNextToDot;

                            // スキーム判定
                            if (i >= 6) {
                                int j = i - 1;
                                if (text.charAt(j--) == '/' && text.charAt(j--) == ':') {
                                    switch (toLower(text.charAt(j--))) {
                                        case 's':
                                            if (!(i >= 7 && toLower(text.charAt(j--)) == 'p'))
                                                break;
                                            // fallthrough
                                        case 'p':
                                            if (toLower(text.charAt(j--)) == 't' && toLower(text.charAt(j--)) == 't' && toLower(text.charAt(j--)) == 'h') {
                                                if (j < 0 || isPrecedingChar(text.charAt(j))) {
                                                    precedingIndex = j;
                                                    hasScheme = true;
                                                    break SchemeCheck;
                                                }
                                            }
                                            break;
                                    }
                                }
                            }

                            break GoToNextToDot;
                        }

                        if (!isValidDomainChar(c)) {
                            if (isPrecedingChar(c)) {
                                precedingIndex = i;
                                break;
                            }

                            // PrecedingChar でないなら無効
                            break GoToNextToDot;
                        }

                        if (lastUnicodeCharIndex == -1 && isUnicodeDomainChar(c))
                            lastUnicodeCharIndex = i;
                    }

                    if (!hasScheme && lastUnicodeCharIndex != -1) {
                        if (lastUnicodeCharIndex != dotIndex - 1 && isPrecedingChar(text.charAt(lastUnicodeCharIndex))) {
                            // Unicode文字を含まないようにして救済
                            precedingIndex = lastUnicodeCharIndex;
                            lastUnicodeCharIndex = -1;
                        } else {
                            break GoToNextToDot;
                        }
                    }

                    x = text.charAt(precedingIndex + 1);
                    if ((precedingIndex == -1 && startIndex != 0) || x == '-' || x == '_')
                        break GoToNextToDot;
                }

                // ホスト部分を最後まで読み取る
                dots.initialize();
                dots.add(dotIndex + 1);
                boolean hasUnicodeCharAfterDot = false;
                int nextIndex = text.length();
                for (int i = dotIndex + 1; i < text.length(); i++) {
                    char c = text.charAt(i);

                    if (c == '.') {
                        // . が text の最後なら終了
                        // スキームなしなのに Unicode 文字が含まれていたら終了
                        if (i == text.length() - 1 || (!hasScheme && hasUnicodeCharAfterDot)) {
                            nextIndex = i;
                            break;
                        }

                        // . の前後の文字が - や _ なら終了
                        x = text.charAt(i - 1);
                        if (x == '-' || x == '_') {
                            nextIndex = i - 1;
                            break;
                        }
                        x = text.charAt(i + 1);
                        if (x == '-' || x == '_') {
                            nextIndex = i;
                            break;
                        }

                        dots.add(i + 1);
                        continue;
                    }

                    if (!isValidDomainChar(c)) {
                        nextIndex = i;
                        break;
                    }

                    if (!hasUnicodeCharAfterDot)
                        hasUnicodeCharAfterDot = isUnicodeDomainChar(c);
                }

                // TLD 検証
                int tldInfo;
                int dotCount;
                TldDecided:
                {
                    for (int i = dots.size() - 1; i >= 0; i--) {
                        int dotIndexPlusOne = dots.get(i);
                        int len = nextIndex - dotIndexPlusOne;
                        if (len < this.shortestTldLength) continue;
                        if (len > this.longestTldLength) len = this.longestTldLength;
                        nextIndex = dotIndexPlusOne + len;

                        hashCodes.initialize();
                        int hash1 = 5381;
                        int hash2 = hash1;

                        for (int j = dotIndexPlusOne; j < nextIndex; ) {
                            hash1 = ((hash1 << 5) + hash1) ^ toLower(text.charAt(j++));
                            hashCodes.add(hash1 + hash2 * 1566083941);
                            if (j >= nextIndex) break;
                            hash2 = ((hash2 << 5) + hash2) ^ toLower(text.charAt(j++));
                            hashCodes.add(hash1 + hash2 * 1566083941);
                        }

                        for (int j = hashCodes.size() - 1; j >= 0; j--) {
                            nextIndex = dotIndexPlusOne + j + 1;
                            if (nextIndex == text.length() || !isAlnumAt(text.charAt(nextIndex))) {
                                tldInfo = this.tldMap.get(hashCodes.get(j));
                                if (tldInfo != IntIntMap4a.NO_VALUE
                                        && nextIndex - dotIndexPlusOne == (tldInfo & 0x3fffffff)) {
                                    dotCount = i + 1;
                                    break TldDecided;
                                }
                            }
                        }
                    }

                    break GoToNextToDot;
                }

                GoToNextIndex:
                {
                    // ccTLD のサブドメインなしはスキーム必須
                    if (!hasScheme && (tldInfo & 0b11 << 30) == CCTLD
                            && (dotCount == 1 && (nextIndex >= text.length() || text.charAt(nextIndex) != '/')))
                        break GoToNextIndex;

                    // サブドメインには _ を使えるがドメインには使えない
                    for (int i = dots.last() - 2; i > precedingIndex; i--) {
                        char c = text.charAt(i);
                        if (c == '.' || c == '/') break;
                        if (c == '_')
                            break GoToNextIndex;
                    }

                    int urlStartIndex = precedingIndex + 1;

                    AddAndGoNext:
                    {
                        if (nextIndex >= text.length())
                            break AddAndGoNext;

                        // ポート番号
                        if (text.charAt(nextIndex) == ':' && ++nextIndex < text.length()) {
                            int portNumberLength = 0;
                            for (; nextIndex < text.length(); nextIndex++) {
                                char c = text.charAt(nextIndex);
                                if (c <= '9' && c >= '0')
                                    portNumberLength++;
                                else
                                    break;
                            }

                            if (portNumberLength == 0) {
                                result.add(new EntityInfo(urlStartIndex, (--nextIndex) - urlStartIndex));
                                break GoToNextIndex;
                            }
                        }

                        if (nextIndex >= text.length())
                            break AddAndGoNext;

                        // パス
                        if (text.charAt(nextIndex) == '/') {
                            nextIndex++;

                            // https?://t.co/xxxxxxxxxx だけ特別扱い
                            int len = nextIndex - urlStartIndex;
                            nextIndex++;
                            if (hasScheme && (len == 11 || len == 12)
                                    && toLower(text.charAt(nextIndex - 2)) == 'o' && toLower(text.charAt(nextIndex - 3)) == 'c'
                                    && text.charAt(nextIndex - 4) == '.' && toLower(text.charAt(nextIndex - 5)) == 't' && text.charAt(nextIndex - 6) == '/'
                                    && nextIndex < text.length() && isAlnum(text.charAt(nextIndex))) {
                                nextIndex++;
                                for (; nextIndex < text.length(); nextIndex++) {
                                    if (!isAlnum(text.charAt(nextIndex)))
                                        break;
                                }
                                break AddAndGoNext;
                            }

                            nextIndex += eatPath(text, nextIndex);
                        }

                        if (nextIndex >= text.length())
                            break AddAndGoNext;

                        // クエリ
                        if (text.charAt(nextIndex) == '?') {
                            nextIndex++;
                            nextIndex += eatQuery(text, nextIndex);
                        }
                    }

                    result.add(new EntityInfo(urlStartIndex, nextIndex - urlStartIndex));
                }

                startIndex = nextIndex;
                continue Start;

            }
            startIndex = dotIndex + 1;
            continue Start;
        }
    }

    public List<EntityInfo> extract(String text) {
        List<EntityInfo> result = new ArrayList<>();
        if (text != null && text.length() > 0)
            this.extract(text, result);
        return result;
    }

    public int getTweetLength(String text, int tcoLength) {
        text = Normalizer.normalize(text, Normalizer.Form.NFC);
        int length = text.codePointCount(0, text.length());
        for (EntityInfo x : this.extract(text)) {
            length += tcoLength - x.getLength();
        }
        return length;
    }
}
