using System.ComponentModel.DataAnnotations;

namespace cloud_dictionary.Shared
{
    public class Definition
    {

        public Definition() { }
        
        public string? Id { get; set; }

        [Required]
        public string Word { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public Author Author { get; set; }
        [Required]
        public string LearnMoreUrl { get; set; }
        [Required]
        public string Tag { get; set; }
        
        public string Abbreviation { get; set; }

    }
}
