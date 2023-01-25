using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using System.Linq;


namespace cloud_dictionary
{

    public class DictionaryRepository
    {
        private readonly Container _definitionsCollection;

        public DictionaryRepository(CosmosClient client, IConfiguration configuration)
        {
            var database = client.GetDatabase(configuration["AZURE_COSMOS_DATABASE_NAME"]);
            _definitionsCollection = database.GetContainer(configuration["AZURE_COSMOS_CONTAINER_NAME"]);

        }

        public async Task<IEnumerable<Definition>> GetDefinitionsAsync(int? skip, int? batchSize)
        {
            return await ToListAsync(
                _definitionsCollection.GetItemLinqQueryable<Definition>(),
                skip,
                batchSize);
        }

        public async Task<Definition?> GetDefinitionAsync(string listId)
        {
            var response = await _definitionsCollection.ReadItemAsync<Definition>(listId, new PartitionKey(listId));
            return response?.Resource;
        }


        public async Task DeleteDefinitionAsync(string definitionId)
        {
            await _definitionsCollection.DeleteItemAsync<Definition>(definitionId, new PartitionKey(definitionId));
        }

        public async Task AddDefinitionAsync(Definition definition)
        {
            definition.Id = Guid.NewGuid().ToString("N");
            await _definitionsCollection.UpsertItemAsync(definition, new PartitionKey(definition.Id));
        }

        public async Task UpdateDefinition(Definition existingDefinition)
        {
            await _definitionsCollection.ReplaceItemAsync(existingDefinition, existingDefinition.Id, new PartitionKey(existingDefinition.Id));
        }
        public async Task<Definition?> GetRandomDefinitionAsync()
        {

            IOrderedQueryable<Definition> linqQueryable = _definitionsCollection.GetItemLinqQueryable<Definition>();
            int count = await linqQueryable.CountAsync();
            var definitions = await GetDefinitionsAsync(null, null);
            int randomIndex = new Random().Next(0, count);
            Definition definition = definitions.ElementAt(randomIndex);
            return definition;

        }

        private async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, int? skip, int? batchSize)
        {
            if (skip != null)
            {
                queryable = queryable.Skip(skip.Value);
            }

            if (batchSize != null)
            {
                queryable = queryable.Take(batchSize.Value);
            }

            using FeedIterator<T> iterator = queryable.ToFeedIterator();
            var items = new List<T>();

            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    items.Add(item);
                }
            }

            return items;
        }

    }
}