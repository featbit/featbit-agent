using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

public class FbDbContext : DbContext
{
    public DbSet<Record> Records => Set<Record>();

    public FbDbContext(DbContextOptions<FbDbContext> builderOptions) : base(builderOptions)
    {
    }

    public void EnsureTableCreated()
    {
        const string script = @"
            CREATE TABLE IF NOT EXISTS Records
            (
                Id        INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Type      TEXT    NOT NULL,
                Content   TEXT    NOT NULL,
                CreatedAt TEXT    NOT NULL
            );
        ";

        Database.ExecuteSqlRaw(script);
    }
}