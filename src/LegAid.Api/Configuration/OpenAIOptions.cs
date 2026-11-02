namespace LegAid.Api.Configuration;

/// <summary>
/// Ustawienia OpenAI. Model jest w appsettings.json,
/// klucz WYŁĄCZNIE w user-secrets lub zmiennej środowiskowej OpenAI__ApiKey.
/// </summary>
public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5-nano";
}
