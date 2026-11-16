using System.Runtime.CompilerServices;
using LegAid.Api.Abstractions;
using LegAid.Api.Logic.Prompts;
using LegAid.Api.Model;
using Microsoft.Extensions.AI;

namespace LegAid.Api.Logic;

public sealed class LegalAgent(
    IChatClient chatClient,
    AgentMessages agentMessages,
    IAnonymizer anonymizer,
    ILogger<LegalAgent> logger) : ILegalAgent
{
    // Ostatnie wiadomości rozmowy, które dostaje model; starsze pomijamy, żeby nie rósł koszt.
    private const int MaxHistoryTurns = 20;

    public async Task<LegalAgentAnswer> AskAsync(
        string message,
        IReadOnlyList<ChatTurn>? history = null,
        CancellationToken cancellationToken = default)
    {
        var response = await chatClient.GetResponseAsync(BuildMessages(message, history), new ChatOptions(), cancellationToken);

        LogUsage(response);

        return new LegalAgentAnswer(response.Text, response.ModelId);
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(
        string message,
        IReadOnlyList<ChatTurn>? history = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<ChatResponseUpdate> updates = [];

        await foreach (var update in chatClient.GetStreamingResponseAsync(BuildMessages(message, history), new ChatOptions(), cancellationToken))
        {
            updates.Add(update);

            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }

        LogUsage(updates.ToChatResponse());
    }

    private List<ChatMessage> BuildMessages(string message, IReadOnlyList<ChatTurn>? history)
    {
        var turns = history ?? [];

        // Anonimizujemy całą rozmowę naraz i przed przycięciem historii, żeby ta sama osoba
        // miała ten sam znacznik we wszystkich wiadomościach i przy każdym pytaniu.
        var texts = anonymizer.Anonymize([.. turns.Select(t => t.Text), message]).Texts;

        var messages = agentMessages.Start(LegalAgentPrompts.System);

        messages.AddRange(turns
            .Select((t, i) => new ChatMessage(t.Role == ChatTurnRole.Assistant ? ChatRole.Assistant : ChatRole.User, texts[i]))
            .TakeLast(MaxHistoryTurns));
        messages.Add(new(ChatRole.User, texts[^1]));

        return messages;
    }

    private void LogUsage(ChatResponse response) =>
        logger.LogInformation(
            "Model {Model}: {InputTokens} tokenów wejścia, {OutputTokens} tokenów wyjścia",
            response.ModelId,
            response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount);
}
