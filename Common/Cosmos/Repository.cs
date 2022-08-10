using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using Polly;
using System.Linq.Expressions;
using System.Net;

namespace Common.Cosmos
{
    public class Repository<T> : IRepository<T>
        where T : class, IEntity
    {
        public Repository(CosmosClient client, IOptions<CosmosNoSQLSettings> options)
        {
            Container = client.GetDatabase(options.Value.DatabaseId).GetContainer(typeof(T).Name);
        }

        public Container Container { get; }

        private const int NUMBER_OF_RETRIES = 3;

        public async Task<T> GetItemAsync(string id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            try
            {
                return await Container.ReadItemAsync<T>(id, new PartitionKey(id));
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var query = Container.GetItemLinqQueryable<T>().Where(predicate).ToFeedIterator();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ReadNextAsync());
            }

            return results;
        }

        public async Task<T> CreateItemAsync(T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrWhiteSpace(item.id))
            {
                item.id = Guid.NewGuid().ToString("N");
            }

            return await Container.CreateItemAsync(item);
        }


        public async Task<T> UpdateItemAsync(string id, T item)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ItemRequestOptions requestOptions = new ItemRequestOptions { IfMatchEtag = item._etag };

            try
            {
                return await Container.ReplaceItemAsync(item, id, null, requestOptions);
            }
            catch (CosmosException cre) when (cre.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new OptimisticConcurrencyException("Record has changed since last known read!", cre);
            }
        }

        public async Task<T> UpdateItemAsync(string id, Action<T> del)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (del is null)
            {
                throw new ArgumentNullException(nameof(del));
            }

            return await UpdateItemAsync(id, x =>
            {
                del(x);
                return Task.CompletedTask;
            });
        }
        public async Task<T> UpdateItemAsync(string id, Func<T, Task> del)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (del is null)
            {
                throw new ArgumentNullException(nameof(del));
            }

            return await Policy
                        .Handle<OptimisticConcurrencyException>()
                        .RetryAsync(NUMBER_OF_RETRIES)
                        .ExecuteAsync(async () =>
                        {
                            var o = await GetItemAsync(id);
                            if (o is null)
                            {
                                return null;
                            }
                            await del(o);
                            return await UpdateItemAsync(id, o);
                        })
                        .ConfigureAwait(false);
        }
        public async Task<T> UpsertItemAsync(T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrWhiteSpace(item.id))
            {
                item.id = Guid.NewGuid().ToString("N");
            }

            ItemRequestOptions requestOptions = new ItemRequestOptions { IfMatchEtag = item._etag };

            try
            {
                return await Container.UpsertItemAsync(item, null, requestOptions);
            }
            catch (CosmosException cre) when (cre.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new OptimisticConcurrencyException("Record has changed since last known read!", cre);
            }
        }

        public async Task<T> DeleteItemAsync(string id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return await Container.DeleteItemAsync<T>(id, new PartitionKey(id));
        }
    }
}