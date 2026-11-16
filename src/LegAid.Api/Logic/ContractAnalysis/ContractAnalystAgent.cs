using System.ComponentModel;
using LegAid.Api.Logic.Prompts;
using Microsoft.Extensions.AI;

namespace LegAid.Api.Logic.ContractAnalysis;

/// <summary>Ogólny obraz umowy od analityka, kontekst dla recenzentów zapisów.</summary>
public sealed record ContractOverview(
    [property: Description("Rodzaj umowy, np. umowa najmu lokalu mieszkalnego")] string ContractType,
    [property: Description("Streszczenie umowy w 2–3 zdaniach")] string Summary,
    [property: Description("Brakujące istotne postanowienia, każde jednym zdaniem z etykietą przepisu")] List<string> MissingClauses);

// Agent 1: czyta całą umowę, określa jej rodzaj, streszcza ją i wskazuje luki.
public sealed class ContractAnalystAgent(IChatClient chatClient, AgentMessages agentMessages)
{
    public async Task<ContractOverview> AnalyzeAsync(string contractText, CancellationToken cancellationToken = default)
    {
        var messages = agentMessages.Start(ContractAnalysisPrompts.Analyst);
        messages.Add(new(ChatRole.User, contractText));

        var response = await chatClient.GetResponseAsync<ContractOverview>(messages, cancellationToken: cancellationToken);

        return response.Result;
    }
}
