using System.Runtime.CompilerServices;
using LegAid.Api.Abstractions;
using LegAid.Api.Logic.Prompts;
using LegAid.Api.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LegAid.Api.Logic;

public sealed class LegalAgent(
    IChatClient chatClient,
    IKnowledgeSource knowledgeSource,
    IOptions<KnowledgeOptions> knowledgeOptions,
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
        List<ChatMessage> messages = [new(ChatRole.System, LegalAgentPrompts.System)];

        // Baza idzie przed historią, żeby początek zapytania był stały i mógł trafić w cache promptu.
        if (knowledgeOptions.Value.Enabled)
        {
            messages.Add(new(ChatRole.System, LegalAgentPrompts.KnowledgeContext(knowledgeSource.GetAll())));
        }

        messages.AddRange((history ?? []).TakeLast(MaxHistoryTurns).Select(t =>
            new ChatMessage(t.Role == ChatTurnRole.Assistant ? ChatRole.Assistant : ChatRole.User, t.Text)));
        messages.Add(new(ChatRole.User, message));

        return messages;
    }

    private void LogUsage(ChatResponse response) =>
        logger.LogInformation(
            "Model {Model}: {InputTokens} tokenów wejścia, {OutputTokens} tokenów wyjścia",
            response.ModelId,
            response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount);
}
