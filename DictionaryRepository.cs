using System;
using cloud_dictionary.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;

namespace cloud_dictionary
{
    public class DictionaryRepository
    {
        private readonly Container _definitionsCollection;
        private readonly Container _definitionOfTheDayCollection;
        private static readonly Random random = new();
        public DictionaryRepository(CosmosClient client, IConfiguration configuration)
        {
            var database = client.GetDatabase(configuration["AZURE_COSMOS_DATABASE_NAME"]);
            _definitionsCollection = database.GetContainer(configuration["AZURE_COSMOS_CONTAINER_NAME"]);
            _definitionOfTheDayCollection = database.GetContainer(configuration["AZURE_COSMOS_DEFINITION_OF_THE_DAY_CONTAINER_NAME"]);
        }
        public async Task<IEnumerable<Definition>> GetDefinitionsAsync(int? skip, int? batchSize)
        {
            return await ToListAsync(
                _definitionsCollection.GetItemLinqQueryable<Definition>(),
                skip,
                batchSize);
        }
        public async Task<IEnumerable<WordDefinition>> GetWordsAsync(int? skip = 0, int? batchSize = 0)
        {
            return await ToListAsync(
                _definitionsCollection.GetItemLinqQueryable<WordDefinition>(),
                skip,
                batchSize);
        }
        public async Task<Definition?> GetDefinitionAsync(string id)
        {
            var response = await _definitionsCollection.ReadItemAsync<Definition>(id, new PartitionKey(id));
            return response?.Resource;
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
        public async Task<List<Definition>> GetDefinitionsByTagAsync(string tag, int? skip, int? batchSize)
        {
            // Set default values for pagination
            int skipValue = skip ?? 0;
            int batchSizeValue = batchSize ?? 100;

            // Compose SQL query with OFFSET and LIMIT for pagination
            var queryDefinition = new QueryDefinition($"SELECT * FROM Definitions d WHERE LOWER(d.tag) = @tag OFFSET {skipValue} LIMIT {batchSizeValue}")
        .WithParameter("@tag", tag.ToLower());

            // Get the iterator
            var iterator = _definitionsCollection.GetItemQueryIterator<Definition>(queryDefinition);

            // Create a list and populate it with the results from the iterator
            var items = new List<Definition>();

            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync();
                items.AddRange(result);
            }

            return items;
        }
        public async Task DeleteDefinitionAsync(string definitionId)
        {
            await _definitionsCollection.DeleteItemAsync<Definition>(definitionId, new PartitionKey(definitionId));
        }
        public async Task AddDefinitionAsync(Definition definition)
        {
            //definition.Id = Guid.NewGuid().ToString("N");
            await _definitionsCollection.CreateItemAsync(definition, new PartitionKey(definition.Id));
        }
       
       
     
        public async Task UpdateDefinition(Definition existingDefinition)
        {
            await _definitionsCollection.ReplaceItemAsync(existingDefinition, existingDefinition.Id, new PartitionKey(existingDefinition.Id));
        }
        public async Task<Definition?> GetRandomDefinitionAsync()
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
        public async Task UpdateListItem(Definition existingItem)
        {
            await _definitionsCollection.ReplaceItemAsync(existingItem, existingItem.Id, new PartitionKey(existingItem.Id));
        }
        private static async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, int? skip, int? batchSize)
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
        public async Task<List<Definition>> GetDefinitionsBySearch(string term, int? skip = 0, int? batchSize = 10)
        {
            // Query in Cosmos DB is case sensitive, so we use ToLower() 
            var queryable = _definitionsCollection.GetItemLinqQueryable<Definition>()
                .Where(d => d.Word.ToLower().Contains(term.ToLower())
                            || d.Content.ToLower().Contains(term.ToLower())
                            || d.Author.Name.ToLower().Contains(term.ToLower())
                            || d.Tag.ToLower().Contains(term.ToLower())
                            || d.Abbreviation.ToLower().Contains(term.ToLower()));

            return await ToListAsync(queryable, skip, batchSize);
        }
        public async Task<int> GetDefinitionCountAsync()
        {

            // Get number of all documents in definitions collection
            var count =  await _definitionsCollection.GetItemLinqQueryable<Definition>().CountAsync();

            return count;
        }

        public async Task<Definition?> GetDefinitionOfTheDay()
        {
            var query = _definitionOfTheDayCollection.GetItemLinqQueryable<Definition>().Take(1).ToFeedIterator();
            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return null;
        }

   
        public async Task UpdateDefinitionOfTheDay(Definition newDefinition)
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


    }
}