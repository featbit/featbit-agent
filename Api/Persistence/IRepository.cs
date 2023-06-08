namespace Api.Persistence;

public interface IRepository
{
    Task AddAsync<TEntity>(TEntity entity) where TEntity : class;

    Task AddManyAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    
    Task<TEntity> FindLastAsync<TEntity>() where TEntity : class;
}