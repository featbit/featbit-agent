namespace Api.Setup;

public static class ConfigurationExtensions
{
    public static bool IsAutoMode(this IConfiguration configuration)
    {
        var mode = configuration.GetValue<string>("Mode") ?? AgentMode.Auto;
        return mode == AgentMode.Auto;
    }
}