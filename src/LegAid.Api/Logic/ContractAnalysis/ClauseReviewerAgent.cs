using LegAid.Api.Logic.Prompts;
using LegAid.Api.Model;
using Microsoft.Extensions.AI;

namespace LegAid.Api.Logic.ContractAnalysis;

// Agent 2: ocenia jeden zapis umowy. Orkiestrator uruchamia go równolegle dla wszystkich zapisów.
public sealed class ClauseReviewerAgent(IChatClient chatClient, AgentMessages agentMessages)
{
    public async Task<IReadOnlyList<ContractRisk>> ReviewAsync(
        ContractOverview overview,
        ContractClause clause,
        CancellationToken cancellationToken = default)
    {
        var messages = agentMessages.Start(ContractAnalysisPrompts.Reviewer);
        messages.Add(new(ChatRole.User, $"""
            Rodzaj umowy: {overview.ContractType}
            Streszczenie: {overview.Summary}

            Zapis do oceny ({clause.Label}):
            {clause.Text}
            """));

        var response = await chatClient.GetResponseAsync<RiskList>(messages, cancellationToken: cancellationToken);

        // Oznaczenie zapisu znamy, więc nie polegamy na tym, co wpisał model.
        return [.. response.Result.Risks.Select(r => r.ToRisk() with { Clause = clause.Label })];
    }
}
