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
    public async Task<LegalAgentAnswer> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages = [new(ChatRole.System, LegalAgentPrompts.System)];

        if (knowledgeOptions.Value.Enabled)
        {
            var fragments = knowledgeBase.Search(message, max: 4);

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
