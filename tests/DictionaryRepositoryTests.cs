using cloud_dictionary.tests.Helpers;
namespace cloud_dictionary.tests;

public class DefinitionsRepositoryTests
{
    private Mock<CosmosClient> _cosmosClientMock;
    private Mock<Container> _definitionContainerMock;
    private Mock<IConfiguration> _configMock;
    private Mock<Database> _databaseMock;

    private DefinitionsRepository _sut;

    public DefinitionsRepositoryTests()
    {
        _cosmosClientMock = new Mock<CosmosClient>();
        _definitionContainerMock = new Mock<Container>();
        _databaseMock = new Mock<Database>();


        _cosmosClientMock.Setup(c => c.GetDatabase(It.IsAny<string>())).Returns(_databaseMock.Object);
        _databaseMock.Setup(d => d.GetContainer(It.IsAny<string>())).Returns(_definitionContainerMock.Object);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["AZURE_COSMOS_DATABASE_NAME"]).Returns("dbname");
        _configMock.Setup(c => c["AZURE_COSMOS_CONTAINER_NAME"]).Returns("containername");


        _sut = new DefinitionsRepository(
            _cosmosClientMock.Object,
            _configMock.Object);
    }

    [Fact]
    public async Task GetDefinitionByIdAsync_ReturnsDefinition()
    {
        // Arrange
        var testDefinition = TestDataGenerator.GenerateDefinition();
        var definitionId = testDefinition.Id;

        _definitionContainerMock.SetupReadItemAsync(testDefinition);

        // Act
        var result = await _sut.GetDefinitionByIdAsync(definitionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(definitionId, result.Id);
    }
}