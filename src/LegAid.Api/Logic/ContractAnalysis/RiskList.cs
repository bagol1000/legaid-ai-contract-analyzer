using System.ComponentModel;
using System.Text.Json.Serialization;
using LegAid.Api.Model;

namespace LegAid.Api.Logic.ContractAnalysis;

// Format odpowiedzi recenzenta i weryfikatora (JSON według schematu). Opisy pól trafiają do schematu dla modelu.
internal sealed record RiskList(List<RiskItem> Risks);

internal sealed record RiskItem(
    [property: Description("Oznaczenie zapisu, np. § 5")] string Clause,
    [property: Description("Na czym polega problem i czym grozi, 1–2 zdania")] string Problem,
    [property: Description("Etykiety przepisów z bazy, np. KC art. 483")] List<string> LegalBasis,
    [property: JsonConverter(typeof(JsonStringEnumConverter<RiskLevel>))] RiskLevel Level,
    [property: Description("Nowe brzmienie zapisu, gotowe do wstawienia do umowy")] string Suggestion)
{
    public ContractRisk ToRisk() => new(Clause, Problem, LegalBasis, Level, Suggestion);

    public static RiskItem From(ContractRisk risk) =>
        new(risk.Clause, risk.Problem, [.. risk.LegalBasis], risk.Level, risk.Suggestion);
}
