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
        var turns = (history ?? []).TakeLast(MaxHistoryTurns).ToList();

        List<ChatMessage> messages = [new(ChatRole.System, LegalAgentPrompts.System)];

        // Baza idzie przed historią, żeby początek zapytania był stały i mógł trafić w cache promptu.
        if (knowledgeOptions.Value.Enabled)
        {
            messages.Add(new(ChatRole.System, LegalAgentPrompts.KnowledgeContext(knowledgeSource.GetAll())));
        }

        messages.AddRange(turns.Select(t =>
            new ChatMessage(t.Role == ChatTurnRole.Assistant ? ChatRole.Assistant : ChatRole.User, t.Text)));
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
