using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using cloud_dictionary.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace cloud_dictionary
{
    public class DictionaryFunctions
    {
        private readonly ILogger _logger;
        private readonly DictionaryRepository _dictionaryRepository;

        public DictionaryFunctions(ILoggerFactory loggerFactory, DictionaryRepository dictionaryRepository)
        {
            _logger = loggerFactory.CreateLogger<DictionaryFunctions>();
            _dictionaryRepository = dictionaryRepository;
        }

        [Function("GetDefinitions")]
        public async Task<HttpResponseData> GetDefinitions(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var definitions = await _dictionaryRepository.GetDefinitionsAsync(null, null);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(definitions));
            return response;
        }

        [Function("GetDefinition")]
        public async Task<HttpResponseData> GetDefinition(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string id)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var definition = await _dictionaryRepository.GetDefinitionAsync(id);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;
        }

        [Function("GetDefinitionByWord")]
        public async Task<HttpResponseData> GetDefinitionByWord(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string word)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var definition = await _dictionaryRepository.GetDefinitionByWordAsync(word);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;
        }

        [Function("GetDefinitionsByTag")]
        public async Task<HttpResponseData> GetDefinitionsByTagAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string tag, int? skip = 0, int? batchSize = 10)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var definition = await _dictionaryRepository.GetDefinitionsByTagAsync(tag, skip, batchSize);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;
        }

        [Function("GetDefinitionsBySearch")]
        public async Task<HttpResponseData> GetDefinitionsBySearch(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string searchTerm)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var definition = await _dictionaryRepository.GetDefinitionsBySearch(searchTerm);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;
        }

        [Function("GetWords")]
        public async Task<HttpResponseData> GetWords(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var words = await _dictionaryRepository.GetWordsAsync(null, null);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(words));
            return response;
        }

        [Function("GetRandomDefinition")]
        public async Task<HttpResponseData> GetRandomDefinition(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var definition = await _dictionaryRepository.GetRandomDefinitionAsync();
            response.WriteString(JsonSerializer.Serialize(definition));

            return response;

        }

        [Function("CreateDefinition")]
        public async Task<HttpResponseData> CreateDefinition(
       [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, string word, string content, string learn_more_url, string tag, string abbreviation, string author_name, string author_link)
        {
            var response = req.CreateResponse(HttpStatusCode.Created);
            var definition = new Definition(word, content, author_name, author_link, learn_more_url, tag, abbreviation);
            await _dictionaryRepository.AddDefinitionAsync(definition);
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;

        }
        [Function("UpdateDefinition")]
        public async Task<HttpResponseData> UpdateDefinition(
           [HttpTrigger(AuthorizationLevel.Function, "put", Route = "{definition_id}")] HttpRequestData req, string definition_id,
           string word, string content, string learn_more_url, string tag, string abbreviation, string author_name, string author_link)
        {
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                var existingItem = await _dictionaryRepository.GetDefinitionAsync(definition_id);
                if (existingItem == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                existingItem.Abbreviation = abbreviation;
                existingItem.Author.Name = author_name;
                existingItem.Author.Link = author_link;
                existingItem.Content = content;
                existingItem.LearnMoreUrl = learn_more_url;
                existingItem.Tag = tag;
                existingItem.Word = word;
                await _dictionaryRepository.UpdateListItem(existingItem);
                return response;
            }

        }

        [Function("GetDefinitionOfTheDay")]
        public async Task<HttpResponseData> GetDefinitionOfTheDay([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var definition = await _dictionaryRepository.GetDefinitionOfTheDay();
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;
        }
        
        [Function("UpdateDefinitionOfTheDay")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
        {

            // Logic to select a random definition
            var definition = await _dictionaryRepository.GetRandomDefinitionAsync();

            // Store the selected definition as 'Definition of the Day'
            await _dictionaryRepository.UpdateDefinitionOfTheDay(definition);
        }

    }
}