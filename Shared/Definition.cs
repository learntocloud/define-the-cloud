namespace cloud_dictionary.Shared
{
    public class Definition
    {
        public Definition(string word, string content, string author_name, string author_link, string learnMoreUrl, string tag, string abbreviation)
        {
            Id = Guid.NewGuid().ToString("N");
            Word = word;
            Content = content;
            Author = new Author(author_name, author_link);
            LearnMoreUrl = learnMoreUrl;
            Tag = tag;
            Abbreviation = abbreviation;
        }
            
        
        public string Id { get; set; }

        public string Word { get; set; }

        public string Content { get; set; }

        public Author Author { get; set; }

        public string LearnMoreUrl { get; set; }

        public string Tag { get; set; }

        public string Abbreviation { get; set; }

    }
}
