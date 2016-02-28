package net.azyobuzi.twitterurlextractor;

public class EntityInfo {
    private int startIndex;
    private int length;

    public int getStartIndex() {
        return this.startIndex;
    }

    public int getLength() {
        return this.length;
    }

    public EntityInfo(int startIndex, int length) {
        this.startIndex = startIndex;
        this.length = length;
    }
}
