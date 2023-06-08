using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

public class SqliteRepository : IRepository
{
    private readonly FbDbContext _context;

    public SqliteRepository(FbDbContext context)
    {
        _context = context;
    }

    public IQueryable<TEntity> QueryableOf<TEntity>() where TEntity : class
        => _context.Set<TEntity>().AsQueryable();

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

    public async Task TruncateAsync<TEntity>() where TEntity : class
    {
        var tableName = _context.Set<TEntity>().EntityType.GetTableName()!;
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM {0}", tableName);
    }
}