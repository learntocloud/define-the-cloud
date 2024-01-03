using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Web;
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

        [Function("GetAllDefinitions")]
        public async Task<HttpResponseData> GetAllDefinitions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, string? continuationToken = null, int? pageSize = 10)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var (definitions, newContinuationToken) = await _dictionaryRepository.GetAllDefinitionsAsync(pageSize, continuationToken);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };

            response.WriteString(JsonSerializer.Serialize(result));
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
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string tag, string? continuationToken = null, int? pageSize = 10)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var (definitions, newContinuationToken) = await _dictionaryRepository.GetDefinitionsByTagAsync(tag, pageSize, continuationToken);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };

            response.WriteString(JsonSerializer.Serialize(result));
            return response;
        }

        [Function("GetDefinitionsBySearch")]
        public async Task<HttpResponseData> GetDefinitionsBySearch(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string searchTerm, string? continuationToken = null, int? pageSize = 10)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            var (definitions, newContinuationToken) = await _dictionaryRepository.GetDefinitionsBySearch(searchTerm, pageSize, continuationToken);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };
            response.WriteString(JsonSerializer.Serialize(result));
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
       [HttpTrigger(AuthorizationLevel.Admin, "post")] HttpRequestData req, string word, string content, string learn_more_url, string tag, string abbreviation, string author_name, string author_link)
        {
            var response = req.CreateResponse(HttpStatusCode.Created);
            var definition = new Definition(word, content, author_name, author_link, learn_more_url, tag, abbreviation);
            await _dictionaryRepository.AddDefinitionAsync(definition);
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;

        }
        [Function("UpdateDefinition")]
        public async Task<HttpResponseData> UpdateDefinition(
           [HttpTrigger(AuthorizationLevel.Admin, "put", Route = "{definition_id}")] HttpRequestData req, string definition_id,
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
                await _dictionaryRepository.UpdateDefinition(existingItem);
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