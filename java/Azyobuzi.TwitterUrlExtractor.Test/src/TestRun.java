import com.twitter.TldLists;
import net.azyobuzi.twitterurlextractor.EntityInfo;
import net.azyobuzi.twitterurlextractor.Extractor;

import java.util.List;

public class TestRun {
    public static void main(String[] args) {
        String s = "twitter.comこれは日本語です。example.com中国語t.co/abcde한국twitter.com example2.comテストtwitter.com/abcde";

        Extractor extractor = new Extractor(TldLists.GTLDS, TldLists.CTLDS);
        List<EntityInfo> result = extractor.extract(s);

        for (EntityInfo x : result) {
            System.out.println(s.substring(x.getStartIndex(), x.getStartIndex() + x.getLength()));
        }
    }
}
