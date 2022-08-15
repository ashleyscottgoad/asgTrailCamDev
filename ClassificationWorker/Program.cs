using API.Models;
using ClassificationWorker;
using Common;
using Microsoft.Extensions.Azure;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfiguration config = builder.Build();

var cosmosConnectionString = config.GetValue<string>("TrailCamCosmosConnectionString");
var serviceBusConnectionString = config.GetValue<string>("TrailCamServiceBusConnectionString");

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddAzureClients(clientsBuilder =>
        {
            clientsBuilder.AddServiceBusClient(serviceBusConnectionString);
        });
        services.AddCosmosDB(cosmosConnectionString).AddSharedRepository<RawImage>();
    })
    .Build();

await host.RunAsync();
