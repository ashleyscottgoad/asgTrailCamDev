using Microsoft.Extensions.DependencyInjection;

namespace Common.Cosmos
{
    public class RepositoryBuilder : IRepositoryBuilder
    {
        public RepositoryBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public IRepositoryBuilder AddSharedRepository<T>()
            where T : class, IEntity
        {
            this.Services.AddSingleton<IRepository<T>, SharedRepository<T>>();
            return this;
        }

    }
}
