namespace LegAid.Api.Model;

public enum RiskLevel
{
    Low,
    Medium,
    High
}

/// <summary>Jeden fragment umowy, np. paragraf.</summary>
/// <param name="Label">Oznaczenie, np. "§ 5" albo "Fragment 2".</param>
public sealed record ContractClause(string Label, string Text);

/// <param name="Clause">Oznaczenie fragmentu umowy, np. "§ 5".</param>
/// <param name="Problem">Na czym polega problem, prostym językiem.</param>
/// <param name="LegalBasis">Etykiety przepisów, np. "KC art. 483".</param>
/// <param name="Suggestion">Proponowane nowe brzmienie zapisu.</param>
public sealed record ContractRisk(
    string Clause,
    string Problem,
    IReadOnlyList<string> LegalBasis,
    RiskLevel Level,
    string Suggestion);

/// <param name="ContractType">Rodzaj umowy, np. "umowa najmu lokalu mieszkalnego".</param>
/// <param name="MissingClauses">Brakujące istotne postanowienia.</param>
/// <param name="RemovedPersonalData">Dane osobowe usunięte przed analizą.</param>
/// <param name="AnonymizedText">Tekst umowy, który dostali agenci.</param>
public sealed record ContractReport(
    string ContractType,
    string Summary,
    IReadOnlyList<ContractRisk> Risks,
    IReadOnlyList<string> MissingClauses,
    IReadOnlyDictionary<PersonalDataKind, int> RemovedPersonalData,
    string AnonymizedText);

public enum ContractAnalysisStage
{
    Anonymizing,
    Analyzing,
    Reviewing,
    Verifying
}

/// <summary>Postęp albo wynik analizy umowy, wysyłany na bieżąco do czatu.</summary>
public abstract record ContractAnalysisUpdate;

/// <param name="Completed">Ile kroków etapu jest gotowych, np. ocenionych paragrafów.</param>
public sealed record ContractAnalysisProgress(ContractAnalysisStage Stage, int Completed, int Total) : ContractAnalysisUpdate;

public sealed record ContractAnalysisCompleted(ContractReport Report) : ContractAnalysisUpdate;
