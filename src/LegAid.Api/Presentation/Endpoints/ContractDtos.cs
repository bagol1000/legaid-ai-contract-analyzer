using System.Text.Json;
using LegAid.Api.Model;

namespace LegAid.Api.Presentation.Endpoints;

// Kształt zdarzeń SSE analizy umowy. Enumy idą jako tekst camelCase, np. "high", "idCard".

/// <param name="Stage">anonymizing, analyzing, reviewing albo verifying.</param>
public sealed record AnalysisProgressDto(string Stage, int Completed, int Total)
{
    public static AnalysisProgressDto From(ContractAnalysisProgress progress) =>
        new(Name(progress.Stage), progress.Completed, progress.Total);

    internal static string Name(Enum value) => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}

/// <param name="Level">low, medium albo high.</param>
public sealed record ContractRiskDto(string Clause, string Problem, IReadOnlyList<string> LegalBasis, string Level, string Suggestion);

/// <param name="RemovedPersonalData">Ile danych osobowych danego rodzaju usunięto przed analizą, np. "person": 2.</param>
/// <param name="AnonymizedText">Tekst umowy po anonimizacji; czat dopisuje go do historii rozmowy.</param>
public sealed record ContractReportDto(
    string ContractType,
    string Summary,
    IReadOnlyList<ContractRiskDto> Risks,
    IReadOnlyList<string> MissingClauses,
    IReadOnlyDictionary<string, int> RemovedPersonalData,
    string AnonymizedText)
{
    public static ContractReportDto From(ContractReport report) => new(
        report.ContractType,
        report.Summary,
        [.. report.Risks.Select(r => new ContractRiskDto(r.Clause, r.Problem, r.LegalBasis, AnalysisProgressDto.Name(r.Level), r.Suggestion))],
        report.MissingClauses,
        report.RemovedPersonalData.ToDictionary(p => AnalysisProgressDto.Name(p.Key), p => p.Value),
        report.AnonymizedText);
}
