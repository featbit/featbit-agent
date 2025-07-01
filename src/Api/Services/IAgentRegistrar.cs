namespace Api.Services;

public interface IAgentRegistrar
{
    Task<string> RegisterAsync(string agentId, CancellationToken cancellationToken = default);
}