using Api.Messaging;
using Api.Store;
using Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Streaming.DependencyInjection;

namespace Api.Setup;

public static class ServicesRegister
{
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder, string[] args)
    {
        var services = builder.Services;

        // add controllers
        services.AddControllers();

        // health check dependencies
        services.AddHealthChecks();

        // cors
        services.AddCors(options => options.AddDefaultPolicy(policyBuilder =>
        {
            policyBuilder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }));

        // streaming
        services.AddStreamingCore()
            .UseNullMessageQueue()
            .UseStore<InMemoryStore>();

        // populate cache
        services.AddHostedService<CachePopulationHostedService>();

        // repository
        services.AddDbContext<FbDbContext>(options => { options.UseSqlite("Data Source=featbit.db"); });
        services.AddScoped<IRepository, SqliteRepository>();
        
        // data change notifier
        services.AddTransient<IDataChangeNotifier, DataChangeNotifier>();

        return builder;
    }
}