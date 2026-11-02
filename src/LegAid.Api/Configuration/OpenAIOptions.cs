namespace LegAid.Api.Configuration;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    // Tylko z user-secrets albo zmiennej OpenAI__ApiKey, nigdy z appsettings.
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}
