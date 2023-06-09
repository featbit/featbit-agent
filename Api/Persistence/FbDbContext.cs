using Api.Store;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

public class FbDbContext : DbContext
{
    public DbSet<Record> Records => Set<Record>();
    public DbSet<SyncHistory> SyncHistories => Set<SyncHistory>();
    public DbSet<StoreItem> StoreItems => Set<StoreItem>();

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