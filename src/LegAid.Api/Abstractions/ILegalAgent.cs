using LegAid.Api.Model;

namespace LegAid.Api.Abstractions;

public interface ILegalAgent
{
    Task<LegalAgentAnswer> AskAsync(
        string message,
        IReadOnlyList<ChatTurn>? history = null,
        CancellationToken cancellationToken = default);
}
