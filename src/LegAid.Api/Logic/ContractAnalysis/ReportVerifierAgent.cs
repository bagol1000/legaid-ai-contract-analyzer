using System.Text.Json;
using LegAid.Api.Logic.Prompts;
using LegAid.Api.Model;
using Microsoft.Extensions.AI;

namespace LegAid.Api.Logic.ContractAnalysis;

// Agent 3: sprawdza wszystkie zgłoszone problemy naraz, usuwa niepotwierdzone przepisami i duplikaty.
public sealed class ReportVerifierAgent(IChatClient chatClient, AgentMessages agentMessages)
{
    public async Task<IReadOnlyList<ContractRisk>> VerifyAsync(
        ContractOverview overview,
        IReadOnlyList<ContractRisk> risks,
        CancellationToken cancellationToken = default)
    {
        if (risks.Count == 0)
        {
            return risks;
        }

        var messages = agentMessages.Start(ContractAnalysisPrompts.Verifier);
        messages.Add(new(ChatRole.User, $"""
            Rodzaj umowy: {overview.ContractType}

            Zgłoszone problemy:
            {JsonSerializer.Serialize(new RiskList([.. risks.Select(RiskItem.From)]), JsonSerializerOptions.Web)}
            """));

        var response = await chatClient.GetResponseAsync<RiskList>(messages, cancellationToken: cancellationToken);

        return [.. response.Result.Risks.Select(r => r.ToRisk())];
    }
}
