using System.Text.Json;
using LegAid.Api.Logic.ContractAnalysis;
using LegAid.Api.Logic.Prompts;
using LegAid.Api.Model;
using Microsoft.Extensions.AI;

public class ContractAnalyzerTests
{
    private const string Contract = """
        UMOWA NAJMU zawarta z Jan Kowalski
        § 1. Przedmiotem najmu jest lokal.
        § 5. Za każdy dzień opóźnienia w zapłacie czynszu Najemca zapłaci karę umowną 1000 zł.
        § 6. Wynajmujący może wypowiedzieć umowę w każdym czasie.
        """;

    [Fact]
    public async Task RunsAgentsInOrderAndBuildsReport()
    {
        var chat = new FakeChatClient(Respond);

        var updates = await AnalyzeAsync(chat, Contract);

        Assert.Equal(
            ["Anonymizing", "Analyzing", "Reviewing 0/3", "Reviewing 1/3", "Reviewing 2/3", "Reviewing 3/3", "Verifying"],
            updates.OfType<ContractAnalysisProgress>().Select(p => p.Stage == ContractAnalysisStage.Reviewing
                ? $"Reviewing {p.Completed}/{p.Total}" : p.Stage.ToString()));

        var report = Assert.IsType<ContractAnalysisCompleted>(updates[^1]).Report;
        Assert.Equal("umowa najmu", report.ContractType);
        Assert.Equal(["§ 5", "§ 6"], report.Risks.Select(r => r.Clause));
        Assert.Equal(RiskLevel.High, report.Risks[0].Level);
        Assert.Equal(["KC art. 483"], report.Risks[0].LegalBasis);
        Assert.Equal(1, report.RemovedPersonalData[PersonalDataKind.Person]);
        Assert.Contains("[OSOBA_1]", report.AnonymizedText);

        Assert.Equal(1 + 3 + 1, chat.Calls.Count);
        Assert.DoesNotContain(chat.Calls.SelectMany(c => c), m => m.Text.Contains("Jan Kowalski"));
    }

    [Fact]
    public async Task SkipsVerifierWhenNoRisksFound()
    {
        var chat = new FakeChatClient(messages => messages[0].Text == ContractAnalysisPrompts.Analyst ? Overview : NoRisks);

        var updates = await AnalyzeAsync(chat, "§ 1. Przedmiotem najmu jest lokal.");

        Assert.Empty(Assert.IsType<ContractAnalysisCompleted>(updates[^1]).Report.Risks);
        Assert.DoesNotContain(chat.Calls, c => c[0].Text == ContractAnalysisPrompts.Verifier);
    }

    private const string Overview = """{"contractType":"umowa najmu","summary":"Najem lokalu.","missingClauses":[]}""";
    private const string NoRisks = """{"risks":[]}""";

    // Recenzent zgłasza problemy w § 5 i § 6, a weryfikator obniża poziom § 6, żeby było widać, że jego wynik trafia do raportu.
    private static string Respond(IReadOnlyList<ChatMessage> messages)
    {
        var instructions = messages[0].Text;
        var input = messages[^1].Text;

        if (instructions == ContractAnalysisPrompts.Analyst)
            return Overview;
        if (instructions == ContractAnalysisPrompts.Verifier)
            return input[input.IndexOf('{')..].Replace("\"medium\"", "\"low\"", StringComparison.OrdinalIgnoreCase);
        if (input.Contains("§ 5."))
            return Risks(new("§ 5", "Kara za opóźnienie w zapłacie jest niedopuszczalna.", ["KC art. 483"], "High", "Odsetki ustawowe."));
        if (input.Contains("§ 6."))
            return Risks(new("§ 6", "Wypowiedzenie bez przyczyny.", [], "Medium", "Wypowiedzenie z ważnych przyczyn."));
        return NoRisks;
    }

    private static string Risks(Risk risk) => JsonSerializer.Serialize(new { risks = new[] { risk } }, JsonSerializerOptions.Web);

    private sealed record Risk(string Clause, string Problem, string[] LegalBasis, string Level, string Suggestion);

    private static async Task<List<ContractAnalysisUpdate>> AnalyzeAsync(FakeChatClient chat, string contract)
    {
        var messages = TestAgents.Messages();
        var analyzer = new ContractAnalyzer(
            new FakeAnonymizer(),
            new ContractAnalystAgent(chat, messages),
            new ClauseReviewerAgent(chat, messages),
            new ReportVerifierAgent(chat, messages));

        List<ContractAnalysisUpdate> updates = [];
        await foreach (var update in analyzer.AnalyzeAsync(contract))
        {
            updates.Add(update);
        }
        return updates;
    }
}
