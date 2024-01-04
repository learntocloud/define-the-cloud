using System.ComponentModel.DataAnnotations;

namespace cloud_dictionary.Shared
{
    public class Author
    {
        public Author(string name, string link)
        {
            Name = name;
            Link = link;
        }

        [Required]
        public string Name { get; set; }
        [Required]
        public string Link { get; set; }
    }
}
