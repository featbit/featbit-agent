using Api.Services;
using Api.Store;
using Microsoft.Extensions.Options;

namespace Api.Setup;

public class AgentRegistrationHostedService(
    IAgentRegistrar registrar,
    IAgentStore agentStore,
    IOptions<AgentOptions> options)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var agentId = await registrar.RegisterAsync(options.Value.AgentId, cancellationToken);
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new Exception(
                "Agent registration failed. " +
                "This is usually due to an invalid API key or network connectivity issues to the streaming server."
            );
        }

        await agentStore.SaveAgentIdAsync(agentId);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}