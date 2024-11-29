using System.ComponentModel.DataAnnotations;
using System.Net;
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
        private readonly DefinitionsRepository _definitionsRepository;
  
        public DictionaryFunctions(ILoggerFactory loggerFactory, DefinitionsRepository definitionsRepository)
        {
            _logger = loggerFactory.CreateLogger<DictionaryFunctions>();
            _definitionsRepository = definitionsRepository;
            
        }

        [Function("GetAllDefinitions")]
        public async Task<HttpResponseData> GetAllDefinitions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, string? continuationToken = null, int? pageSize = 10)
        {
            var (definitions, newContinuationToken) = await _definitionsRepository.GetAllDefinitionsAsync(pageSize, continuationToken);
            if (definitions == null || !definitions.Any())
            {
                _logger.LogInformation("No definitions found.");
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = "No definitions found." });
            }
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };
            _logger.LogInformation("All definitions retrieved successfully.");
            return await CreateJsonResponse(req, HttpStatusCode.OK, result);
        }

        [Function("GetDefinitionById")]
        public async Task<HttpResponseData> GetDefinitionById(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string id, string word)
        {
            var definition = await _definitionsRepository.GetDefinitionByIdAsync(id, word);
            if (definition == null)
            {
                _logger.LogInformation($"No definition found for ID: {id}");
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definition found for ID {id}." });
            }
            _logger.LogInformation($"Definition retrieved for ID: {id}");
            return await CreateJsonResponse(req, HttpStatusCode.OK, definition);
        }

        [Function("GetDefinitionByWord")]
        public async Task<HttpResponseData> GetDefinitionByWord(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string word)
        {
            var definition = await _definitionsRepository.GetDefinitionByWordAsync(word);
            if (definition == null)
            {
                _logger.LogInformation($"No definition found for word: {word}");
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definition found for word {word}." });
            }
            _logger.LogInformation($"Definition retrieved for word: {word}");
            return await CreateJsonResponse(req, HttpStatusCode.OK, definition);
        }

        [Function("GetProjectByWord")]
        public async Task<HttpResponseData> GetProjectByWord(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string word)
        {
            var project = await _definitionsRepository.GetProjectByWordAsync(word);
            if (project == null)
            {
                _logger.LogInformation($"No project found for word: {word}");
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No project found for word {word}." });
            }
            _logger.LogInformation($"Definition retrieved for word: {word}");
            return await CreateJsonResponse(req, HttpStatusCode.OK, project);
        }

        [Function("GetDefinitionsByTag")]
        public async Task<HttpResponseData> GetDefinitionsByTagAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string tag, string? continuationToken = null, int? pageSize = 5)
        {
            var (definitions, newContinuationToken) = await _definitionsRepository.GetDefinitionsByTagAsync(tag, pageSize, continuationToken);
            if (!definitions.Any())
            {
                _logger.LogInformation($"No definitions found for tag {tag}.");
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definitions found for tag {tag}." });
            }
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };
            _logger.LogInformation($"Definitions retrieved for tag {tag}.");
            return await CreateJsonResponse(req, HttpStatusCode.OK, result);
        }

        [Function("GetDefinitionsBySearch")]
        public async Task<HttpResponseData> GetDefinitionsBySearch(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequestData req, string searchTerm, string? continuationToken = null, int? pageSize = 10)
        {
            var (definitions, newContinuationToken) = await _definitionsRepository.GetDefinitionsBySearch(searchTerm, pageSize, continuationToken);
            if (!definitions.Any())
            {
                _logger.LogInformation($"No definitions found for search term {searchTerm}.");
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definitions found for search term {searchTerm}." });
            }
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };
            _logger.LogInformation($"Definitions retrieved for search term {searchTerm}.");
            return await CreateJsonResponse(req, HttpStatusCode.OK, result);
        }

        [Function("DeleteDefinition")]
        public async Task<HttpResponseData> DeleteDefinition(
            [HttpTrigger(AuthorizationLevel.Admin, "delete")] HttpRequestData req, string word)
        {

            var existingDefinition = await _definitionsRepository.GetDefinitionByWordAsync(word);
            if (existingDefinition == null)
            {
                _logger.LogInformation($"DeleteDefinition: Definition with word {word} not found.");
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"Definition with word {word} not found." });
            }
            await _definitionsRepository.DeleteDefinitionAsync(existingDefinition);
            _logger.LogInformation($"Definition with {word} deleted successfully.");
            return req.CreateResponse(HttpStatusCode.NoContent);
        }




        [Function("CreateDefinition")]
        public async Task<HttpResponseData> CreateDefinition(
    [HttpTrigger(AuthorizationLevel.Admin, "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogInformation("Request body is null or empty.");
                return await CreateJsonResponse(req, HttpStatusCode.BadRequest, new { Error = "Request body is null or empty." });
            }
            Definition? newDefinition = JsonSerializer.Deserialize<Definition>(requestBody);
            if (newDefinition == null || !Validator.TryValidateObject(newDefinition, new ValidationContext(newDefinition), null))
            {
                _logger.LogInformation("Invalid data in request body.");
                return await CreateJsonResponse(req, HttpStatusCode.BadRequest, new { Error = "Invalid data in request body." });
            }
            var existingDefinition = await _definitionsRepository.GetDefinitionByWordAsync(newDefinition.Word);
            if (existingDefinition != null)
            {
                _logger.LogInformation($"A definition with the same word {newDefinition.Word} already exists.");
                return await CreateJsonResponse(req, HttpStatusCode.Conflict, new { Error = $"A definition for {newDefinition.Word} already exists." });
            }
            await _definitionsRepository.AddDefinitionAsync(newDefinition);
            _logger.LogInformation($"Definition with id {newDefinition.Id} created successfully.");
            return await CreateJsonResponse(req, HttpStatusCode.Created, newDefinition);
        }

        [Function("UpdateDefinition")]
        public async Task<HttpResponseData> UpdateDefinition(
        [HttpTrigger(AuthorizationLevel.Admin, "put", Route = "UpdateDefinition")] HttpRequestData req, string word)
        {
            var existingDefinition = await _definitionsRepository.GetDefinitionByWordAsync(word);
            if (existingDefinition == null)
            {
                _logger.LogInformation($"UpdateDefinition: Definition with word {word} not found.");
                var errorContent = new { Error = $"Definition with word {word} not found." };
                return await CreateJsonResponse(req, HttpStatusCode.NotFound, errorContent);
            }
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogInformation("DictionaryFunctions.UpdateDefinition: Request body is null or empty.");
                var errorContent = new { Error = "Request body is null or empty." };
                return await CreateJsonResponse(req, HttpStatusCode.BadRequest, errorContent);
            }
            Definition? data = JsonSerializer.Deserialize<Definition>(requestBody);
            if (data == null || !Validator.TryValidateObject(data, new ValidationContext(data), null))
            {
                _logger.LogInformation("DictionaryFunctions.UpdateDefinition: Invalid data in request body.");
                var errorContent = new { Error = "Invalid data in request body." };
                return await CreateJsonResponse(req, HttpStatusCode.BadRequest, errorContent);
            }
            await _definitionsRepository.UpdateDefinition(data);
            _logger.LogInformation($"Definition with word {word} updated successfully.");
            return await CreateJsonResponse(req, HttpStatusCode.OK, data);
        }

        


        private async Task<HttpResponseData> CreateJsonResponse<T>(HttpRequestData req, HttpStatusCode statusCode, T content, string contentType="application/json; charset=utf-8")
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", contentType);

            if (content is byte[] byteArray)
            {
                await response.Body.WriteAsync(byteArray);

            } else
            {
                response.WriteString(JsonSerializer.Serialize(content));
            }

            
            return response;
        }

        
            
               
        }
    }
