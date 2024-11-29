using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ILogger logger = context.GetLogger("ExceptionHandlingMiddleware");
        try
        {
            await next(context);
        }
        catch (CosmosException cosmosEx)
        {
            // Handle Cosmos DB related exceptions
            logger.LogError(cosmosEx, "CosmosException occurred.");
            await HandleErrorResponse(context, HttpStatusCode.InternalServerError, "A database error occurred.");
        }
        catch (ValidationException validationEx)
        {
            // Handle validation exceptions
            logger.LogError(validationEx, "ValidationException occurred.");
            await HandleErrorResponse(context, HttpStatusCode.BadRequest, validationEx.Message);
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            logger.LogError(ex, "An unhandled exception occurred.");
            await HandleErrorResponse(context, HttpStatusCode.InternalServerError, "An internal server error occurred.");
        }
    }

    private static async Task HandleErrorResponse(FunctionContext context, HttpStatusCode statusCode, string message)
    {
        if (context.BindingContext.BindingData.TryGetValue("HttpResponseData", out var httpResponseBinding) && httpResponseBinding is HttpResponseData response)
        {
            response.StatusCode = statusCode;
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var errorResponse = new { error = message };
            await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}
