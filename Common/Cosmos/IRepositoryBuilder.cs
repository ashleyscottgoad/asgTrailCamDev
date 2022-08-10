namespace Common.Cosmos
{
    public interface IRepositoryBuilder
    {
        IRepositoryBuilder AddSharedRepository<T>() where T : class, IEntity;
    }
}
