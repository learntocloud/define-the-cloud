using System;
using System.Web;
using cloud_dictionary.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;

namespace cloud_dictionary
{
    public class DefinitionsRepository
    {
        private readonly Container _definitionsCollection;
        private const int MaxPageSize = 50;
        private static readonly Random random = new();
        public DefinitionsRepository(CosmosClient client, IConfiguration configuration)
        {
            var database = client.GetDatabase(configuration["AZURE_COSMOS_DATABASE_NAME"]);
            _definitionsCollection = database.GetContainer(configuration["AZURE_COSMOS_CONTAINER_NAME"]);
        }
        public async Task<(IEnumerable<Definition>, string?)> GetAllDefinitionsAsync(int? pageSize, string? continuationToken)
        {
            string query = "SELECT * FROM c";
            pageSize = pageSize.HasValue && pageSize.Value <= MaxPageSize ? pageSize.Value : MaxPageSize;

            return await QueryWithPagingAsync<Definition>(query, pageSize, continuationToken);
        }
        public async Task<Definition?> GetDefinitionByIdAsync(string id, string word)
        {
            var response = await _definitionsCollection.ReadItemAsync<Definition>(id, new PartitionKey(word));
            return response.Resource;
        }
        public async Task<Definition?> GetDefinitionByWordAsync(string word)
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
        public async Task<(IEnumerable<Definition>, string?)> GetDefinitionsByTagAsync(string tag, int? pageSize, string? continuationToken)
        {
            var query = $"SELECT * FROM Definitions d WHERE LOWER(d.tag) = '{tag.ToLower()}'";
            pageSize = pageSize.HasValue && pageSize.Value <= MaxPageSize ? pageSize.Value : MaxPageSize;
            return await QueryWithPagingAsync<Definition>(query, pageSize, continuationToken);
        }
        public async Task DeleteDefinitionAsync(Definition definition)
        {
            await _definitionsCollection.DeleteItemAsync<Definition>(definition.Id, new PartitionKey(definition.Word));
        }
        public async Task AddDefinitionAsync(Definition definition)
        {
            definition.Id = Guid.NewGuid().ToString("N");
            await _definitionsCollection.CreateItemAsync(definition, new PartitionKey(definition.Word));
        }
        public async Task UpdateDefinition(Definition existingDefinition)
        {
            await _definitionsCollection.ReplaceItemAsync(existingDefinition, existingDefinition.Id, new PartitionKey(existingDefinition.Word));
        }
        
        private async Task<(List<T>, string?)> QueryWithPagingAsync<T>(string query, int? pageSize, string? continuationToken)
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
                string? encodedContinuationToken =
                response.ContinuationToken != null ? HttpUtility.UrlEncode(response.ContinuationToken) : null;
                continuationToken = encodedContinuationToken;
                if (response.Count <= pageSize) { break; }
            }
            return (entities, continuationToken);
        }

        public async Task<(IEnumerable<Definition>, string?)> GetDefinitionsBySearch(string searchTerm, int? pageSize, string? continuationToken)
        {
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

        public async Task<Definition?> GetRandomDefinitionAsync()
        {
            int count = await GetDefinitionCountAsync();
            // Use DefinitionService.cs to get the count of definitions

            int randomIndex = random.Next(0, count);
            var query = _definitionsCollection.GetItemLinqQueryable<Definition>()
                .Skip(randomIndex)
                .Take(1)
                .ToFeedIterator();

            List<Definition> definitions = new();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                definitions.AddRange(response.ToList());
            }
            return definitions.FirstOrDefault();
        }

        public async Task<int> GetDefinitionCountAsync()
        {
            var count = await _definitionsCollection.GetItemLinqQueryable<Definition>().CountAsync();
            return count;
        }

    }
}