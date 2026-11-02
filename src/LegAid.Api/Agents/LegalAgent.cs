using Microsoft.Extensions.AI;

namespace LegAid.Api.Agents;

public sealed class LegalAgent(IChatClient chatClient, ILogger<LegalAgent> logger) : ILegalAgent
{
    public async Task<LegalAgentAnswer> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, LegalAgentPrompts.System),
            new(ChatRole.User, message)
        ];

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
