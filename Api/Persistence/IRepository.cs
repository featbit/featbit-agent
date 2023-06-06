namespace Api.Persistence;

public interface IRepository
{
    Task AddManyAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
}