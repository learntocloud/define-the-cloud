using System.Net;
using System.Web;
using cloud_dictionary.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace cloud_dictionary
{
    public class DictionaryRepository
    {
        private readonly Container _definitionsCollection;
        private readonly Container _definitionOfTheDayCollection;

        private readonly ILogger _logger;
        private const int MaxPageSize = 50;
        private static readonly Random random = new();
        public DictionaryRepository(ILoggerFactory loggerFactory, CosmosClient client, IConfiguration configuration)
        {
            var database = client.GetDatabase(configuration["AZURE_COSMOS_DATABASE_NAME"]);
            _definitionsCollection = database.GetContainer(configuration["AZURE_COSMOS_CONTAINER_NAME"]);
            _definitionOfTheDayCollection = database.GetContainer(configuration["AZURE_COSMOS_DEFINITION_OF_THE_DAY_CONTAINER_NAME"]);
            _logger = loggerFactory.CreateLogger<DictionaryFunctions>();
        }
        public async Task<(IEnumerable<Definition>, string?)> GetAllDefinitionsAsync(int? pageSize, string? continuationToken)
        {
            try
            {
                string query = "SELECT * FROM c";
                pageSize = pageSize.HasValue && pageSize.Value <= MaxPageSize ? pageSize.Value : MaxPageSize;

                return await QueryWithPagingAsync<Definition>(query, pageSize, continuationToken);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, $"A Cosmos DB error occurred while getting the definitions.");
                return (null, null)!;
            }
        }
        public async Task<Definition?> GetDefinitionByIdAsync(string id)
        {
            try
            {
                var response = await _definitionsCollection.ReadItemAsync<Definition>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        public async Task<Definition?> GetDefinitionByWordAsync(string word)
        {
            try
            {
                var queryDefinition = new QueryDefinition("SELECT * FROM Definitions d WHERE LOWER(d.word) = @word").WithParameter("@word", word.ToLower());
                var queryResultSetIterator = _definitionsCollection.GetItemQueryIterator<Definition>(queryDefinition);
                List<Definition> definitions = new();

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<Definition> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (Definition definition in currentResultSet)
                    {
                        definitions.Add(definition);
                    }
                }

                return definitions.FirstOrDefault(); // since 'word' is unique, there should be only one match
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, $"A Cosmos DB error occurred while getting the definition for word {word}.");
                return null;
            }

        }
        public async Task<(IEnumerable<Definition>, string?)> GetDefinitionsByTagAsync(string tag, int? pageSize, string? continuationToken)
        {
            try
            {
                var query = $"SELECT * FROM Definitions d WHERE LOWER(d.tag) = '{tag.ToLower()}'";
                pageSize = pageSize.HasValue && pageSize.Value <= MaxPageSize ? pageSize.Value : MaxPageSize;

                return await QueryWithPagingAsync<Definition>(query, pageSize, continuationToken);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, $"A Cosmos DB error occurred while getting the definitions for tag {tag}.");
                return (null, null)!;
            }
        }
        public async Task DeleteDefinitionAsync(string definitionId)
        {
            try
            {
                await _definitionsCollection.DeleteItemAsync<Definition>(definitionId, new PartitionKey(definitionId));
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, $"A Cosmos DB error occurred while deleting the definition with id {definitionId}.");
                throw;
            }
        }
        public async Task AddDefinitionAsync(Definition definition)
        {
            try
            {
                definition.Id = Guid.NewGuid().ToString("N");
                await _definitionsCollection.CreateItemAsync(definition, new PartitionKey(definition.Id));
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, $"A Cosmos DB error occurred while adding the definition with id {definition.Id}.");
                throw;
            }
        }
        public async Task UpdateDefinition(Definition existingDefinition)
        {
            try
            {
                await _definitionsCollection.ReplaceItemAsync(existingDefinition, existingDefinition.Id, new PartitionKey(existingDefinition.Id));
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, $"A Cosmos DB error occurred while updating the definition with id {existingDefinition.Id}.");
                throw;
            }
        }
        public async Task<Definition?> GetRandomDefinitionAsync()
        {
            try
            {
                int count = await GetDefinitionCountAsync();
                int randomIndex = random.Next(0, count);
                // Query to get the random document
                var query = _definitionsCollection.GetItemLinqQueryable<Definition>()
                    .Skip(randomIndex)
                    .Take(1)
                    .ToFeedIterator();

                // Execute the query
                List<Definition> definitions = new();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    definitions.AddRange(response.ToList());
                }

                return definitions.FirstOrDefault();
            }
            catch (CosmosException ex)
            {
               _logger.LogError(ex, $"A Cosmos DB error occurred while getting a random definition.");
                return null;
            }
        }
        private async Task<(List<T>, string?)> QueryWithPagingAsync<T>(string query, int? pageSize, string? continuationToken)
        {

            try
            {
                List<T> entities = new List<T>(); // Create a local list of type <T> objects.
                QueryDefinition queryDefinition = new QueryDefinition(query);

                using FeedIterator<T> resultSetIterator = _definitionsCollection.GetItemQueryIterator<T>(
                    queryDefinition,
                    continuationToken,
                    new QueryRequestOptions() { MaxItemCount = pageSize });

                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<T> response = await resultSetIterator.ReadNextAsync();
                    entities.AddRange(response);

                    string? encodedContinuationToken = response.ContinuationToken != null ? HttpUtility.UrlEncode(response.ContinuationToken) : null;


                    continuationToken = encodedContinuationToken;

                    if (response.Count <= pageSize) { break; }
                }
                return (entities, continuationToken);
            }
            catch (CosmosException ex)
            {
                // Implement appropriate error handling
                _logger.LogError(ex, $"A Cosmos DB error occurred while getting the definitions.");
                throw;
            }
        }
        public async Task<(IEnumerable<Definition>, string?)> GetDefinitionsBySearch(string searchTerm, int? pageSize, string? continuationToken)
        {
            try
            {
                // Query in Cosmos DB is case sensitive, so we use ToLower() 
                IQueryable<Definition> queryable = _definitionsCollection.GetItemLinqQueryable<Definition>()
                    .Where(d => d.Word.ToLower().Contains(searchTerm.ToLower())
                                || d.Content.ToLower().Contains(searchTerm.ToLower())
                                || d.Author.Name.ToLower().Contains(searchTerm.ToLower())
                                || d.Tag.ToLower().Contains(searchTerm.ToLower())
                                || d.Abbreviation.ToLower().Contains(searchTerm.ToLower()));
                pageSize = pageSize.HasValue && pageSize.Value <= MaxPageSize ? pageSize.Value : MaxPageSize;

                string query = queryable.ToQueryDefinition().QueryText;
                return await QueryWithPagingAsync<Definition>(query, pageSize, continuationToken);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, $"A Cosmos DB error occurred while getting the definitions for search term {searchTerm}.");
                return (null, null)!;
            }
        }
        public async Task<int> GetDefinitionCountAsync()
        {
            try
            {
                // Get number of all documents in definitions collection
                var count = await _definitionsCollection.GetItemLinqQueryable<Definition>().CountAsync();

                return count;
            }
            catch (CosmosException ex)
            {
                // Log the error or rethrow it, depending on your error handling strategy
                Console.WriteLine($"CosmosException occurred: {ex.Message}");
                throw;
            }
        }
        public async Task<Definition?> GetDefinitionOfTheDay()
        {
            try
            {
                var query = _definitionOfTheDayCollection.GetItemLinqQueryable<Definition>().Take(1).ToFeedIterator();
                if (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    return response.FirstOrDefault();
                }
                return null;
            }
            catch (CosmosException ex)
            {
                // Log the error or rethrow it, depending on your error handling strategy
                Console.WriteLine($"CosmosException occurred while getting definition of the day: {ex.Message}");
                return null;
            }
        }
        public async Task UpdateDefinitionOfTheDay(Definition newDefinition)
        {
            try
            {
                // Fetch the current 'Definition of the Day', if it exists
                var query = _definitionOfTheDayCollection.GetItemLinqQueryable<Definition>().Take(1).ToFeedIterator();
                Definition currentDefinition = null;
                if (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    currentDefinition = response.FirstOrDefault();
                }

                // Delete the current definition if it exists
                if (currentDefinition != null)
                {
                    await _definitionOfTheDayCollection.DeleteItemAsync<Definition>(currentDefinition.Id, new PartitionKey(currentDefinition.Id));
                }

                // Add the new 'Definition of the Day'
                await _definitionOfTheDayCollection.UpsertItemAsync(newDefinition, new PartitionKey(newDefinition.Id));
            }
            catch (CosmosException ex)
            {
                // Log the error or rethrow it, depending on your error handling strategy
                Console.WriteLine($"CosmosException occurred: {ex.Message}");
                throw;
            }
        }

    }
}
