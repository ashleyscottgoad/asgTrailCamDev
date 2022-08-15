using Azure.Messaging.ServiceBus;
using ClassificationWorker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    //.AddJsonFile(args.Where(a => a.Contains("SettingsFile")).First().Split("=")[1], optional: false, reloadOnChange: true)
    ;

IConfiguration config = builder.Build();

var serviceBusConnectionString = config.GetValue<string>("TrailCamServiceBusConnectionString");

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddAzureClients(clientsBuilder =>
        {
            clientsBuilder.AddServiceBusClient(serviceBusConnectionString);
        });
    })
    .Build();

await host.RunAsync();
