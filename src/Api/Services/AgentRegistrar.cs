using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Api.Setup;
using Api.Shared;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Api.Services;

public class AgentRegistrar(
    IOptions<AgentOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<AgentRegistrar> logger)
    : IAgentRegistrar
{
    private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(10);
    private readonly int _maxAttempts = 5;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2);

    private readonly AgentOptions _options = options.Value;

    public async Task<string> RegisterAsync(string agentId, CancellationToken cancellationToken = default)
    {
        StringContent payload = new(
            JsonSerializer.Serialize(agentId),
            Encoding.UTF8,
            MediaTypeNames.Application.Json
        );

        for (var i = 0; i < _maxAttempts; i++)
        {
            bool isRecoverable;
            string error;
            using var timeoutCts = new CancellationTokenSource(_requestTimeout);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                logger.LogInformation("Attempting to register agent...");

                var client = CreateClient();

                var response = await client.PostAsync(
                    "/api/public/agent/register",
                    payload,
                    cts.Token
                );

                response.EnsureSuccessStatusCode();

                var responseAgentId = await response.Content.ReadAsStringAsync(cts.Token);

                logger.LogInformation("Agent registered successfully with ID: {AgentId}", responseAgentId);
                return responseAgentId;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case HttpRequestException hre:
                        isRecoverable = HttpErrors.IsRecoverable((int?)hre.StatusCode);
                        error = hre.Message;
                        break;
                    case OperationCanceledException when !cts.IsCancellationRequested:
                        isRecoverable = true;
                        error = "Request timed out.";
                        break;
                    default:
                        isRecoverable = true;
                        error = ex.Message;
                        break;
                }
            }

            if (!isRecoverable)
            {
                logger.LogError("Unrecoverable error during agent registration: {Error}", error);
                break;
            }

            logger.LogWarning(
                "Error during agent registration (attempt {Attempt}/{MaxAttempts}): {Error}. Retrying in {RetryDelay} seconds...",
                i + 1, _maxAttempts, error, _retryDelay.TotalSeconds
            );

            try
            {
                await Task.Delay(_retryDelay, cts.Token);
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("Retry delay was canceled. Stopping registration attempts.");
                break;
            }
        }

        return string.Empty;

        HttpClient CreateClient()
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_options.StreamingUri
                .Replace("ws://", "http://")
                .Replace("wss://", "https://")
            );
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _options.ApiKey);

            return httpClient;
        }
    }
}