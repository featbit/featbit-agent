using Api.Store;
using Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Setup;

public class CachePopulationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CachePopulationHostedService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

        var storeItems =
            await repository.QueryableOf<StoreItem>().ToListAsync(cancellationToken);

        InMemoryStore.Populate(storeItems);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}