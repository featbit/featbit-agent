using Api.Shared;
using Api.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Persistence;

public class FbDbContext : DbContext
{
    public DbSet<Record> Records => Set<Record>();
    public DbSet<SyncHistory> SyncHistories => Set<SyncHistory>();
    public DbSet<StoreItem> StoreItems => Set<StoreItem>();

    public FbDbContext(DbContextOptions<FbDbContext> builderOptions) : base(builderOptions)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<UtcValueConverter>();
    }

    // ref: https://github.com/dotnet/efcore/issues/4711#issuecomment-1048572602
    class UtcValueConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcValueConverter()
            : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
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
            CREATE TABLE IF NOT EXISTS StoreItems
            (
                Id        TEXT    NOT NULL PRIMARY KEY,
                EnvId     TEXT    NOT NULL,
                Type      TEXT    NOT NULL,
                Timestamp INTEGER NOT NULL,
                JsonBytes BLOB    NOT NULL
            );
        ";

        Database.ExecuteSqlRaw(script);
    }
}