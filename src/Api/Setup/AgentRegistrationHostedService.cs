using Api.Services;
using Api.Store;
using Microsoft.Extensions.Options;

namespace Api.Setup;

public class AgentRegistrationHostedService(
    IAgentRegistrar registrar,
    IAgentStore agentStore,
    IOptions<AgentOptions> options,
    ILogger<AgentRegistrationHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Mode != AgentMode.Auto)
        {
            logger.LogInformation("Agent is not in auto mode, skipping registration.");
            return;
        }

        var agentId = await registrar.RegisterAsync(options.Value.AgentId, cancellationToken);
        if (string.IsNullOrWhiteSpace(agentId))
        {
            logger.LogWarning(
                "Agent registration failed. " +
                "This is usually due to an invalid API key or network connectivity issues to the streaming server."
            );
            return;
        }

        await agentStore.SaveAgentIdAsync(agentId);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}