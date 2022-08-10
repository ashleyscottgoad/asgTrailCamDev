using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace Common.Cosmos
{
    public interface IRepository<T> where T : class, IEntity
    {
        Container Container { get; }
        Task<T> CreateItemAsync(T item);
        Task<T> DeleteItemAsync(string id);
        Task<T> GetItemAsync(string id);
        Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate);
        Task<T> UpdateItemAsync(string id, Action<T> del);
    }
}