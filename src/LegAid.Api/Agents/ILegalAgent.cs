namespace LegAid.Api.Agents;

public interface ILegalAgent
{
    Task<LegalAgentAnswer> AskAsync(
        string message,
        IReadOnlyList<ChatTurn>? history = null,
        CancellationToken cancellationToken = default);
}

public sealed record LegalAgentAnswer(string Text, string? Model);

/// <param name="Role">"user" albo "assistant".</param>
/// <param name="Text">Treść wiadomości.</param>
public sealed record ChatTurn(string Role, string Text);
