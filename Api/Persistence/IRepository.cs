namespace Api.Persistence;

public interface IRepository
{
    Task AddManyAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    Task AddAsync<TEntity>(TEntity entity) where TEntity : class;
}