using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

public class FbDbContext : DbContext
{
    public DbSet<Record> Records => Set<Record>();
    public DbSet<SyncHistory> SyncHistories => Set<SyncHistory>();
    
    public FbDbContext(DbContextOptions<FbDbContext> builderOptions) : base(builderOptions)
    {
    }

    public void EnsureTableCreated()
    {
        const string script = @"
            CREATE TABLE IF NOT EXISTS Records
            (
                Id        INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Content   TEXT    NOT NULL,
                CreatedAt TEXT    NOT NULL
            );
            CREATE TABLE IF NOT EXISTS SyncHistories
            (
                Id        INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CreatedAt TEXT    NOT NULL
            );
        ";

        Database.ExecuteSqlRaw(script);
    }
}