using Api.Persistence;
using Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Streaming.DependencyInjection;

namespace Api;

public static class ServicesRegister
{
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder, string[] args)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // add ini config
        configuration.AddIniFile("featbit.ini", optional: false, reloadOnChange: false);

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
            .UseRedisStore(options => configuration.GetSection(RedisOptions.Redis).Bind(options));

        // sqlite
        services.AddDbContext<FbDbContext>(options => { options.UseSqlite("Data Source=featbit.db"); });
        services.AddScoped<IRepository, SqliteRepository>();

        return builder;
    }
}