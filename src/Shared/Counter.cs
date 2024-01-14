namespace cloud_dictionary.Shared
{
    public class Counter
    {
        public Counter(string id, int count)
        {
            Id = id;
            Count = count;
        }

        public string Id { get; set; }

        public int Count { get; set; }
    }

}