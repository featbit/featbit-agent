using Api.DataSynchronizer;
using Api.Messaging;
using Api.Services;
using Api.Store;
using Domain.Messages;
using Domain.Shared;
using Streaming.Connections;
using Streaming.DependencyInjection;

namespace Api.Setup;

public static class ServicesRegister
{
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        // add controllers
        services.AddControllers();

        // health check dependencies
        services.AddHealthChecks()
            .AddCheck<DataSynchronizerHealthCheck>("DataSynchronizer");

        // cors
        services.AddCors(options => options.AddDefaultPolicy(policyBuilder =>
        {
            policyBuilder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }));

        // streaming
        services.AddStreamingCore(x =>
        {
            x.SupportedTypes = [ConnectionType.Server, ConnectionType.Client];
            x.CustomRpService = new NoopRelayProxyService();
        });

        var memoryStore = new InMemoryStore();
        services.AddSingleton<IStore>(memoryStore);
        services.AddSingleton<IAgentStore>(memoryStore);

        services.AddSingleton<IMessageProducer, NoneMessageProducer>();

        // starting data synchronizer
        services.AddSingleton<IDataSynchronizer, WebSocketDataSynchronizer>();
        services.AddHostedService<DataSynchronizerHostedService>();

        // data change notifier
        services.AddTransient<IDataChangeNotifier, DataChangeNotifier>();

        return builder;
    }
}