namespace Api.Setup;

public static class AgentMode
{
    public const string Auto = "auto";
    public const string Manual = "manual";

    public static string[] All { get; } = [Auto, Manual];

    public static bool IsDefined(string mode) => All.Contains(mode);
}