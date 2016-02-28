package net.azyobuzi.twitterurlextractor;

class IntList {
    private int[] array;
    private int count;

    public void initialize() {
        if (this.array == null)
            this.array = new int[4];
        this.count = 0;
    }

    public void add(int value) {
        if (this.array.length == this.count) {
            int[] newArray = new int[this.count * 2];
            System.arraycopy(this.array, 0, newArray, 0, this.count);
            this.array = newArray;
        }

        this.array[this.count++] = value;
    }

    public int get(int index) {
        return this.array[index];
    }

    public int size() {
        return this.count;
    }

    public int last() {
        return this.array[this.count - 1];
    }
}
