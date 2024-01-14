
namespace cloud_dictionary.tests.Helpers;
public class TestDataGenerator
{
    public static Definition GenerateDefinition()
    {
        return new Definition()
        {
            Id = Guid.NewGuid().ToString(),
            Word = "Test Word",
            Content = "Test Content",
            Author = new Author("Author Name", "https://author.example.com"),
            LearnMoreUrl = "https://example.com",
            Tag = "Test Tag",
            Abbreviation = "TW"
        };
    }

}
