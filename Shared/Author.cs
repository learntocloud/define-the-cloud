namespace cloud_dictionary.Shared
{
    public class Author
    {
        public Author(string name, string link)
        {
            Name = name;
            Link = link;
        }
        
        
        public string Name { get; set; }

        public string Link { get; set; }
    }
}
