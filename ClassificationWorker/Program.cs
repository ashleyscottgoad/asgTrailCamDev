using API.Models;
using ClassificationWorker;
using Common;
using Microsoft.Extensions.Azure;
using Microsoft.ML;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfiguration config = builder.Build();

var storageConnectionString = config.GetValue<string>("TrailCamStorageConnectionString");
var cosmosConnectionString = config.GetValue<string>("TrailCamCosmosConnectionString");
var serviceBusConnectionString = config.GetValue<string>("TrailCamServiceBusConnectionString");

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddAzureClients(clientsBuilder =>
        {
            clientsBuilder.AddBlobServiceClient(storageConnectionString, preferMsi: true);
            clientsBuilder.AddServiceBusClient(serviceBusConnectionString);
        });
        services.AddCosmosDB(cosmosConnectionString).AddSharedRepository<RawImage>();
        services.AddSingleton<MLContext>();
    })
    .Build();

await host.RunAsync();
