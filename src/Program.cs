using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.CognitiveServices.Speech;

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
                            services.AddSingleton<DefinitionsRepository>();
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
                            services.AddSingleton(sp =>
                            {
                                var config = SpeechConfig.FromSubscription(Environment.GetEnvironmentVariable("SpeechServiceKey"), Environment.GetEnvironmentVariable("SpeechServiceRegion"));



                                config.SpeechSynthesisVoiceName = "en-US-JennyNeural";


                                return new SpeechSynthesizer(config);

                            });
                        })
                        .Build();
        await host.RunAsync();

    }
}