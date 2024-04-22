using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.SemanticKernel;

namespace cloud_dictionary;
class Program
{
    static async Task Main(string[] args)
    {


        var host = new HostBuilder()
                        .ConfigureFunctionsWorkerDefaults(worker =>
                        {
                            worker.UseNewtonsoftJson();
                            worker.UseMiddleware<ExceptionHandlingMiddleware>();
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton<DefinitionRepository>();
                            services.AddSingleton<DefinitionOfTheDayRepository>();
                            services.AddSingleton<IKernelBuilder>(sp =>
                            {
                                var builder = Kernel.CreateBuilder();

                                builder.AddAzureOpenAIChatCompletion(
                                    "completionsmodel", // Azure OpenAI Deployment Name
                                    "endpoint", // Azure OpenAI Endpoint
                                    "apiKey" // Azure OpenAI Key
                                );

                                return builder;
                            });
                            services.AddSingleton(sp =>
                            { 

                                return new CosmosClient(Environment.GetEnvironmentVariable("AZURE_COSMOS_ENDPOINT"), new CosmosClientOptions
                                {
                                    SerializerOptions = new CosmosSerializationOptions
                                    {
                                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                                    }
                                });
                            });
                        })
                        .Build();
        await host.RunAsync();

    }
}