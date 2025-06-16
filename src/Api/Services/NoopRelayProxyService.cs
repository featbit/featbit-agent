using Domain.Shared;
using Streaming.Services;

namespace Api.Services;

public class NoopRelayProxyService : IRelayProxyService
{
    public Task<SecretWithValue[]> GetSecretsAsync(string key) => Task.FromResult(Array.Empty<SecretWithValue>());

    public Task<Secret[]> GetServerSecretsAsync(string key) => Task.FromResult(Array.Empty<Secret>());
}