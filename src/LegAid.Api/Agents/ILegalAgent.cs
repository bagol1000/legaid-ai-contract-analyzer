namespace LegAid.Api.Agents;

public interface ILegalAgent
{
    Task<LegalAgentAnswer> AskAsync(string message, CancellationToken cancellationToken = default);
}

public sealed record LegalAgentAnswer(string Text, string? Model);
