using Microsoft.Extensions.Options;

namespace Api.Setup;

public class AgentOptions
{
    public string Mode { get; set; } = AgentMode.Auto;

    public string AgentId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string StreamingUri { get; set; } = string.Empty;

    public bool ForwardEvents { get; set; } = true;

    public string? EventUri { get; set; } = string.Empty;
}

public class AgentOptionsValidation : IValidateOptions<AgentOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentOptions options)
    {
        var mode = options.Mode;
        if (!AgentMode.IsDefined(mode))
        {
            return ValidateOptionsResult.Fail(
                $"Agent mode '{mode}' is not defined. Supported modes are: {string.Join(", ", AgentMode.All)}."
            );
        }

        if (mode == AgentMode.Auto && string.IsNullOrWhiteSpace(options.AgentId))
        {
            return ValidateOptionsResult.Fail("AgentId is required when the mode is set to 'Auto'.");
        }

        var apiKey = options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ValidateOptionsResult.Fail("ApiKey is not configured.");
        }

        var streamingUri = options.StreamingUri;
        if (string.IsNullOrWhiteSpace(streamingUri))
        {
            return ValidateOptionsResult.Fail("StreamingUri is not configured.");
        }

        if (!Uri.TryCreate(streamingUri, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("StreamingUri is not a valid absolute URI.");
        }

        var eventUri = options.EventUri;
        if (options.ForwardEvents)
        {
            if (string.IsNullOrWhiteSpace(eventUri))
            {
                return ValidateOptionsResult.Fail("EventUri is required when 'ForwardEvents' is enabled.");
            }

            if (!Uri.TryCreate(eventUri, UriKind.Absolute, out _))
            {
                return ValidateOptionsResult.Fail("EventUri is not a valid absolute URI.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}