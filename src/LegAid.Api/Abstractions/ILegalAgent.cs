using LegAid.Api.Model;

namespace LegAid.Api.Abstractions;

public interface ILegalAgent
{
    Task<LegalAgentAnswer> AskAsync(
        string message,
        IReadOnlyList<ChatTurn>? history = null,
        CancellationToken cancellationToken = default);

    // Ta sama odpowiedź co AskAsync, ale w kawałkach tekstu, w miarę jak model ją generuje.
    IAsyncEnumerable<string> AskStreamingAsync(
        string message,
        IReadOnlyList<ChatTurn>? history = null,
        CancellationToken cancellationToken = default);
}
