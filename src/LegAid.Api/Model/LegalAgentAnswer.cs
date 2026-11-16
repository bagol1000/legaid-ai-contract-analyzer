namespace LegAid.Api.Model;

/// <param name="Text">Odpowiedź agenta.</param>
/// <param name="Model">Model, który ją wygenerował.</param>
public sealed record LegalAgentAnswer(string Text, string? Model);
