namespace Api.Persistence;

public interface IRepository
{
    IQueryable<TEntity> QueryableOf<TEntity>() where TEntity : class;

    Task AddAsync<TEntity>(TEntity entity) where TEntity : class;

    Task AddManyAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    Task TruncateAsync<TEntity>() where TEntity : class;
}