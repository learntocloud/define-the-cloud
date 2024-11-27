using System.ComponentModel.DataAnnotations;

namespace cloud_dictionary.Shared
{
    public class Project
    {
        public Project() { }
        
        public string? Id { get; set; }

        [Required]
        public string Word { get; set; }
        [Required]
        public string Description { get; set; }
    }
}
