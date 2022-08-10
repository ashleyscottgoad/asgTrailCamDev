using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core.Extensions;
using Microsoft.Extensions.Azure;
using Common.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Common
{
    public static class ServiceCollectionExtensions
    {
        /*
        public static IServiceCollection AddMessageQueue(this IServiceCollection services,
            IConfiguration messageQueueConfiguration)
        {
            services.Configure<MessageQueueConfiguration>(messageQueueConfiguration);
            services.AddSingleton<IConnectionFactory>(p =>
            {
                var options = p.GetRequiredService<IOptions<MessageQueueConfiguration>>();

                return new ConnectionFactory
                {
                    HostName = options.Value.HostName,
                    Port = options.Value.Port,
                    UserName = options.Value.UserName,
                    Password = options.Value.Password,
                    DispatchConsumersAsync = true
                };
            });
            services.AddSingleton<IPersistentConnection, DefaultRabbitMQPersistentConnection>();
            return services;
        }

        [Obsolete("The Publisher Configuration parameter has never been honored. Use another AddPublisher overload.")]
        public static IServiceCollection AddPublisher(this IServiceCollection services,
            IConfiguration publisherConfiguration)
        {
            //services.Configure<MessageQueuePublisherOptions>(publisherConfiguration);
            services.AddSingleton<IMessageQueuePublisher, MessageQueuePublisher>();
            return services;
        }
        public static IServiceCollection AddPublisher(this IServiceCollection services)
        {

            services.AddSingleton<IMessageQueuePublisher, MessageQueuePublisher>();
            return services;
        }


        public static IServiceCollection AddSubscriber(this IServiceCollection services,
            IConfiguration subscriberConfiguration)
        {
            services.Configure<MessageQueueSubscriberOptions>(subscriberConfiguration);
            services.AddSingleton<IMessageQueueSubscriber, MessageQueueSubscriber>();
            return services;
        }
        */

        public static IRepositoryBuilder AddCosmosDB(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton(p =>
            {
                return new CosmosClient(connectionString);
            });
            return new RepositoryBuilder(services);
        }

        public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri? serviceUri))
            {
                return builder.AddBlobServiceClient(serviceUri);
            }
            else
            {
                return builder.AddBlobServiceClient(serviceUriOrConnectionString);
            }
        }
    }
}
