namespace Api.Persistence;

public class SqliteRepository : IRepository
{
    private readonly FbDbContext _context;

    public SqliteRepository(FbDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync<TEntity>(TEntity entity) where TEntity : class
    {
        _context.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddManyAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        _context.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public Task<TEntity> FindLastAsync<TEntity>() where TEntity : class
    {
        throw new NotImplementedException();
    }
}