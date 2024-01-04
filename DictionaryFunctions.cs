using System.ComponentModel.DataAnnotations;
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

        [Function("GetDefinitionById")]
        public async Task<HttpResponseData> GetDefinitionById(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string id)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            var definition = await _dictionaryRepository.GetDefinitionByIdAsync(id);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(definition));
            return response;
        }

        [Function("GetDefinitionByWord")]
        public async Task<HttpResponseData> GetDefinitionByWord(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string word)
        {

            try
            {
                var definition = await _dictionaryRepository.GetDefinitionByWordAsync(word);
                if (definition == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    notFoundResponse.WriteString($"No definition found for word {word}.");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonSerializer.Serialize(definition));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while getting the definition for word {word}.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.WriteString("An error occurred. Please try again later.");
                return errorResponse;
            }
        }

        [Function("GetDefinitionsByTag")]
        public async Task<HttpResponseData> GetDefinitionsByTagAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string tag, string? continuationToken = null, int? pageSize = 10)
        {
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
       [HttpTrigger(AuthorizationLevel.Admin, "post")] HttpRequestData req)
        {
            try
            {

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogInformation("In DictionaryFunctions.CreateDefinition: Request body is null or empty.");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                Definition? newDefinition = JsonSerializer.Deserialize<Definition>(requestBody);
                if (newDefinition == null || !Validator.TryValidateObject(newDefinition, new ValidationContext(newDefinition), null))
                {
                    _logger.LogInformation("In DictionaryFunctions.CreateDefinition: Invalid data in request body.");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                // Check if a definition with the same contents already exists
                var existingDefinition = await _dictionaryRepository.GetDefinitionByWordAsync(newDefinition.Word);
                if (existingDefinition != null)
                {
                    _logger.LogInformation("CreateDefinition: A definition with the same word already exists.");
                    var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                    conflictResponse.WriteString($"A definition for {newDefinition.Word} already exists.");
                    return conflictResponse;
                }

                await _dictionaryRepository.AddDefinitionAsync(newDefinition);
                _logger.LogInformation($"Definition with id {newDefinition.Id} created successfully.");
                return req.CreateResponse(HttpStatusCode.Created);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateDefinition: An error occurred while creating the definition.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.WriteString("An error occurred while processing your request. Please try again later.");
                return errorResponse;
            }
        }
        [Function("UpdateDefinition")]
        public async Task<HttpResponseData> UpdateDefinition(
            [HttpTrigger(AuthorizationLevel.Admin, "put", Route = "UpdateDefinition/{definition_id}")] HttpRequestData req, string definition_id)
        {
            try
            {
                var existingDefinition = await _dictionaryRepository.GetDefinitionByIdAsync(definition_id);
                if (existingDefinition == null)
                {
                    _logger.LogInformation($"UpdateDefinition: Definition with id {definition_id} not found.");
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogInformation("DictionaryFunctions.UpdateDefinition: Request body is null or empty.");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                Definition? data = JsonSerializer.Deserialize<Definition>(requestBody);
                if (data == null || !Validator.TryValidateObject(data, new ValidationContext(data), null))
                {
                    _logger.LogInformation("DictionaryFunctions.UpdateDefinition: Invalid data in request body.");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                await _dictionaryRepository.UpdateDefinition(data);
                _logger.LogInformation($"Definition with id {definition_id} updated successfully.");
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DictionaryFunctions: An error occurred while updating the definition.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.WriteString("An error occurred while processing your request. Please try again later.");
                return errorResponse;
            }
        }

        [Function("GetDefinitionOfTheDay")]
        public async Task<HttpResponseData> GetDefinitionOfTheDay([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
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
            Definition? definition = await _dictionaryRepository.GetRandomDefinitionAsync();

            // Store the selected definition as 'Definition of the Day'
            await _dictionaryRepository.UpdateDefinitionOfTheDay(definition);
        }

    }
}