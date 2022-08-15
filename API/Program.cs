using API.Models;
using Common;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

//var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri"));
//builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var storageConnectionString = builder.Configuration.GetValue<string>("TrailCamStorageConnectionString");
var cosmosConnectionString = builder.Configuration.GetValue<string>("TrailCamCosmosConnectionString");
var cosmosDatabaseName = builder.Configuration.GetValue<string>("TrailCamCosmosDatabaseName");
var serviceBusConnectionString = builder.Configuration.GetValue<string>("TrailCamServiceBusConnectionString");

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(storageConnectionString, preferMsi: true);
    clientBuilder.AddServiceBusClient(serviceBusConnectionString);
});

builder.Services.AddCosmosDB(cosmosConnectionString).AddSharedRepository<RawImage>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
