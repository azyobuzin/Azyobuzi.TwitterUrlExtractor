namespace Azyobuzi.TwitterUrlExtractor
{
    public struct EntityInfo
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }

        public EntityInfo(int startIndex, int length)
        {
            this.StartIndex = startIndex;
            this.Length = length;
        }
    }
}
