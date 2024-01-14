using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using cloud_dictionary.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace cloud_dictionary
{
    public class DictionaryFunctions
    {
        private readonly ILogger _logger;
        private readonly DefinitionsRepository _definitionsRepository;
        private readonly DefinitionOfTheDayRepository _definitionOfTheDayRepository;
        public DictionaryFunctions(ILoggerFactory loggerFactory, DefinitionsRepository definitionsRepository, DefinitionOfTheDayRepository definitionOfTheDayRepository)
        {
            _logger = loggerFactory.CreateLogger<DictionaryFunctions>();
            _definitionsRepository = definitionsRepository;
            _definitionOfTheDayRepository = definitionOfTheDayRepository;
        }

        [OpenApiOperation(operationId: "GetAllDefinitions", tags: new[] { "Definitions" }, Summary = "Retrieve All Definitions", Description = "Retrieves a paginated list of all definitions. Supports pagination through continuation tokens.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Definition), Summary = "List of Definitions", Description = "Returns a paginated list of definitions along with a continuation token for further pages.")]
        [Function("GetAllDefinitions")]
        public async Task<HttpResponseData> GetAllDefinitions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, string? continuationToken = null, int? pageSize = 10)
        {
            var (definitions, newContinuationToken) = await _definitionsRepository.GetAllDefinitionsAsync(pageSize, continuationToken);
            if (definitions == null || !definitions.Any())
            {
                _logger.LogInformation("No definitions found.");
                return CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = "No definitions found." });
            }
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };
            _logger.LogInformation("All definitions retrieved successfully.");
            return CreateJsonResponse(req, HttpStatusCode.OK, result);
        }

        [Function("GetDefinitionById")]
        [OpenApiOperation(operationId: "GetDefinitionById", tags: new[] { "Definitions" }, Summary = "Get definition by ID", Description = "This operation returns a definition by its ID.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The ID of the definition.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Definition), Summary = "Successful response", Description = "This returns the definition.")]
        public async Task<HttpResponseData> GetDefinitionById(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string id)
        {
            var definition = await _definitionsRepository.GetDefinitionByIdAsync(id);
            if (definition == null)
            {
                _logger.LogInformation($"No definition found for ID: {id}");
                return CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definition found for ID {id}." });
            }
            _logger.LogInformation($"Definition retrieved for ID: {id}");
            return CreateJsonResponse(req, HttpStatusCode.OK, definition);
        }

        [Function("GetDefinitionByWord")]
        [OpenApiOperation(operationId: "GetDefinitionByWord", tags: new[] { "Definitions" }, Summary = "Get definition by word", Description = "This operation returns a definition by its word.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "word", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The word of the definition.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Definition), Summary = "Successful response", Description = "This returns the definition.")]
        public async Task<HttpResponseData> GetDefinitionByWord(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string word)
        {
            var definition = await _definitionsRepository.GetDefinitionByWordAsync(word);
            if (definition == null)
            {
                _logger.LogInformation($"No definition found for word: {word}");
                return CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definition found for word {word}." });
            }
            _logger.LogInformation($"Definition retrieved for word: {word}");
            return CreateJsonResponse(req, HttpStatusCode.OK, definition);
        }

        [Function("GetDefinitionsByTag")]
        [OpenApiOperation(operationId: "GetDefinitionsByTag", tags: new[] { "Definitions" }, Summary = "Get definitions by tag", Description = "This operation returns definitions associated with a specific tag.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "tag", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The tag to get the definitions for.")]
        [OpenApiParameter(name: "continuationToken", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The continuation token to get the next page of definitions.")]
        [OpenApiParameter(name: "pageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "The number of definitions to return per page.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Dictionary<string, Definition>), Summary = "Successful response", Description = "This returns a list of definitions associated with the tag.")]
        public async Task<HttpResponseData> GetDefinitionsByTagAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string tag, string? continuationToken = null, int? pageSize = 10)
        {
            var (definitions, newContinuationToken) = await _definitionsRepository.GetDefinitionsByTagAsync(tag, pageSize, continuationToken);
            if (!definitions.Any())
            {
                _logger.LogInformation($"No definitions found for tag {tag}.");
                return CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definitions found for tag {tag}." });
            }
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };
            _logger.LogInformation($"Definitions retrieved for tag {tag}.");
            return CreateJsonResponse(req, HttpStatusCode.OK, result);
        }

        [Function("GetDefinitionsBySearch")]
        [OpenApiOperation(operationId: "GetDefinitionsBySearch", tags: new[] { "Definitions" }, Summary = "Get definitions by search term", Description = "This operation returns definitions matching a search term.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "searchTerm", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The search term to get the definitions for.")]
        [OpenApiParameter(name: "continuationToken", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The continuation token to get the next page of definitions.")]
        [OpenApiParameter(name: "pageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "The number of definitions to return per page.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Dictionary<string, Definition>), Summary = "Successful response", Description = "This returns a list of definitions matching the search term.")]
        public async Task<HttpResponseData> GetDefinitionsBySearch(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequestData req, string searchTerm, string? continuationToken = null, int? pageSize = 10)
        {
            var (definitions, newContinuationToken) = await _definitionsRepository.GetDefinitionsBySearch(searchTerm, pageSize, continuationToken);
            if (!definitions.Any())
            {
                _logger.LogInformation($"No definitions found for search term {searchTerm}.");
                return CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = $"No definitions found for search term {searchTerm}." });
            }
            var result = new
            {
                Data = definitions,
                ContinuationToken = newContinuationToken
            };
            _logger.LogInformation($"Definitions retrieved for search term {searchTerm}.");
            return CreateJsonResponse(req, HttpStatusCode.OK, result);
        }


        [Function("GetRandomDefinition")]
        [OpenApiOperation(operationId: "GetRandomDefinition", tags: new[] { "Definitions" }, Summary = "Get a random definition", Description = "This operation returns a random definition.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Definition), Summary = "Successful response", Description = "This returns a random definition.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "Internal Server Error", Description = "When an error occurs while processing the request.")]
        public async Task<HttpResponseData> GetRandomDefinition(
    [HttpTrigger(AuthorizationLevel.Admin, "get")] HttpRequestData req)
        {
            var definition = await _definitionsRepository.GetRandomDefinitionAsync();
            if (definition == null)
            {
                _logger.LogWarning("No random definition could be found.");
                return CreateJsonResponse(req, HttpStatusCode.NotFound, new { Error = "No random definition could be found." });
            }
            _logger.LogInformation("Random definition retrieved successfully.");
            return CreateJsonResponse(req, HttpStatusCode.OK, definition);
        }

        [Function("CreateDefinition")]
        [OpenApiOperation(operationId: "CreateDefinition", tags: new[] { "Definitions" }, Summary = "Create a new definition", Description = "This operation creates a new definition.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody("application/json", typeof(Definition), Required = true, Description = "The new definition to create.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(Definition), Summary = "Successful response", Description = "This returns the created definition.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request", Description = "When the request body is null, empty or invalid.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Conflict", Description = "When a definition with the same word already exists.")]
        public async Task<HttpResponseData> CreateDefinition(
    [HttpTrigger(AuthorizationLevel.Admin, "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogInformation("Request body is null or empty.");
                return CreateJsonResponse(req, HttpStatusCode.BadRequest, new { Error = "Request body is null or empty." });
            }
            Definition? newDefinition = JsonSerializer.Deserialize<Definition>(requestBody);
            if (newDefinition == null || !Validator.TryValidateObject(newDefinition, new ValidationContext(newDefinition), null))
            {
                _logger.LogInformation("Invalid data in request body.");
                return CreateJsonResponse(req, HttpStatusCode.BadRequest, new { Error = "Invalid data in request body." });
            }
            var existingDefinition = await _definitionsRepository.GetDefinitionByWordAsync(newDefinition.Word);
            if (existingDefinition != null)
            {
                _logger.LogInformation($"A definition with the same word {newDefinition.Word} already exists.");
                return CreateJsonResponse(req, HttpStatusCode.Conflict, new { Error = $"A definition for {newDefinition.Word} already exists." });
            }
            await _definitionsRepository.AddDefinitionAsync(newDefinition);
            _logger.LogInformation($"Definition with id {newDefinition.Id} created successfully.");
            return CreateJsonResponse(req, HttpStatusCode.Created, newDefinition);
        }

        [Function("UpdateDefinition")]
        [OpenApiOperation(operationId: "UpdateDefinition", tags: new[] { "Definitions" }, Summary = "Update an existing definition", Description = "This operation updates an existing definition.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "definition_id", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The ID of the definition to update.")]
        [OpenApiRequestBody("application/json", typeof(Definition), Required = true, Description = "The updated definition.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Definition), Summary = "Successful response", Description = "This returns the updated definition.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Definition not found", Description = "When no definition is found with the provided ID.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request", Description = "When the request body is null, empty or invalid.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "Internal Server Error", Description = "When an error occurs while processing the request.")]
        public async Task<HttpResponseData> UpdateDefinition(
    [HttpTrigger(AuthorizationLevel.Admin, "put", Route = "UpdateDefinition/{definition_id}")] HttpRequestData req, string definition_id)
        {
            var existingDefinition = await _definitionsRepository.GetDefinitionByIdAsync(definition_id);
            if (existingDefinition == null)
            {
                _logger.LogInformation($"UpdateDefinition: Definition with id {definition_id} not found.");
                var errorContent = new { Error = $"Definition with id {definition_id} not found." };
                return CreateJsonResponse(req, HttpStatusCode.NotFound, errorContent);
            }
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogInformation("DictionaryFunctions.UpdateDefinition: Request body is null or empty.");
                var errorContent = new { Error = "Request body is null or empty." };
                return CreateJsonResponse(req, HttpStatusCode.BadRequest, errorContent);
            }
            Definition? data = JsonSerializer.Deserialize<Definition>(requestBody);
            if (data == null || !Validator.TryValidateObject(data, new ValidationContext(data), null))
            {
                _logger.LogInformation("DictionaryFunctions.UpdateDefinition: Invalid data in request body.");
                var errorContent = new { Error = "Invalid data in request body." };
                return CreateJsonResponse(req, HttpStatusCode.BadRequest, errorContent);
            }
            await _definitionsRepository.UpdateDefinition(data);
            _logger.LogInformation($"Definition with id {definition_id} updated successfully.");
            return CreateJsonResponse(req, HttpStatusCode.OK, data);
        }

        [Function("GetDefinitionOfTheDay")]
        [OpenApiOperation(operationId: "GetDefinitionOfTheDay", tags: new[] { "Definitions" }, Summary = "Get definition of the day", Description = "This operation returns the definition of the day.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Definition), Summary = "Successful response", Description = "This returns the definition of the day.")]
        public async Task<HttpResponseData> GetDefinitionOfTheDay(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var definition = await _definitionOfTheDayRepository.GetDefinitionOfTheDay();
            if (definition == null)
            {
                var errorResult = new { Error = "No definition found for word." };
                return CreateJsonResponse(req, HttpStatusCode.NotFound, errorResult);
            }
            var resultWithoutToken = new { Data = definition };
            return CreateJsonResponse(req, HttpStatusCode.OK, resultWithoutToken);
        }

        [Function("UpdateDefinitionOfTheDay")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
        {
            Definition? definition = await _definitionOfTheDayRepository.GetRandomDefinitionAsync();

            if (definition == null)
            {
                _logger.LogError("UpdateDefinitionOfTheDay: No definition could be selected.");
                return;
            }
            await _definitionOfTheDayRepository.UpdateDefinitionOfTheDay(definition);
            _logger.LogInformation("UpdateDefinitionOfTheDay: Definition of the day updated successfully.");
        }

        private HttpResponseData CreateJsonResponse<T>(HttpRequestData req, HttpStatusCode statusCode, T content)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(content));
            return response;
        }
    }
}