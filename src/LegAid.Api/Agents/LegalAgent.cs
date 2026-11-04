using LegAid.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LegAid.Api.Agents;

public sealed class LegalAgent(
    IChatClient chatClient,
    KnowledgeBase knowledgeBase,
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
        var turns = (history ?? []).TakeLast(MaxHistoryTurns).ToList();

        List<ChatMessage> messages = [new(ChatRole.System, LegalAgentPrompts.System)];
        messages.AddRange(turns.Select(t =>
            new ChatMessage(t.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, t.Text)));

        if (knowledgeOptions.Value.Enabled)
        {
            // Pytanie typu "a co z § 7?" samo niewiele mówi, więc szukamy też po poprzednim pytaniu.
            var previousQuestion = turns.LastOrDefault(t => t.Role == "user")?.Text;
            var fragments = knowledgeBase.Search($"{previousQuestion}\n{message}", max: 4);

            logger.LogInformation("Baza wiedzy: znalezione przepisy ({Count}): {Sources}",
                fragments.Count, string.Join(", ", fragments.Select(f => f.Source)));

            if (fragments.Count > 0)
            {
                messages.Add(new(ChatRole.System, LegalAgentPrompts.KnowledgeContext(fragments)));
            }
        }

        messages.Add(new(ChatRole.User, message));

        var options = new ChatOptions();

        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);

        logger.LogInformation(
            "Model {Model}: {InputTokens} tokenów wejścia, {OutputTokens} tokenów wyjścia",
            response.ModelId,
            response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount);

        return new LegalAgentAnswer(response.Text, response.ModelId);
    }
}
