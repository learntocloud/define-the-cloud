using System.Web;
using cloud_dictionary.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace cloud_dictionary.Repositories
{
    public class ProjectRepository
    {
        private readonly Container _projectCollection;
        private IKernelBuilder _builder;

        
        public ProjectRepository(CosmosClient client, IConfiguration configuration, IKernelBuilder builder)
        {
            var database = client.GetDatabase(configuration["AZURE_COSMOS_DATABASE_NAME"]);
            _projectCollection = database.GetContainer(configuration["AZURE_COSMOS_PROJECT_CONTAINER_NAME"]);
            _builder = builder;
        }
        public async Task<Project?> GetProjectByWordAsync(string word)
        {
            var queryDefinition = new QueryDefinition("SELECT * FROM Project d WHERE LOWER(d.word) = @word").WithParameter("@word", word.ToLower());
            var queryResultSetIterator = _projectCollection.GetItemQueryIterator<Project>(queryDefinition);
            List<Project> projects = new();
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Project> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Project project in currentResultSet)
                {
                    projects.Add(project);
                }
            }
            return projects.FirstOrDefault();
        }

        
    }
}
