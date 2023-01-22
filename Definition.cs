using System.Text.Json.Serialization;

namespace cloud_dictionary
{
    public class Definition
    {
        public string Id { get; set; }

        public string Word { get; set; }

        public string Content { get; set; }

        public Author Author { get; set; }

        public string LearnMoreUrl { get; set; }

        public string Tag { get; set; }

        public string Abbreviation { get; set; }

    }
}
