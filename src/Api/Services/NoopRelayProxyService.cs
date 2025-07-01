using Domain.Shared;
using Streaming.Services;

namespace Api.Services;

public class NoopRelayProxyService : IRelayProxyService
{
    public Task<bool> IsKeyValidAsync(string key) => Task.FromResult(false);

    public Task<SecretWithValue[]> GetSecretsAsync(string key) => Task.FromResult(Array.Empty<SecretWithValue>());

    public Task<Secret[]> GetServerSecretsAsync(string key) => Task.FromResult(Array.Empty<Secret>());

    public Task RegisterAgentAsync(string key, string agentId) => Task.CompletedTask;

    public Task UpdateAgentStatusAsync(string key, string agentId, string status) => Task.CompletedTask;
}