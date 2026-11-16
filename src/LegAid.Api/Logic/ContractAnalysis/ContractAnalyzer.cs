using System.Runtime.CompilerServices;
using LegAid.Api.Abstractions;
using LegAid.Api.Model;

namespace LegAid.Api.Logic.ContractAnalysis;

// Orkiestrator analizy umowy, sterowany kodem: anonimizacja → analityk → recenzenci zapisów (równolegle) → weryfikator.
// Na bieżąco zwraca postęp, a na końcu gotowy raport.
public sealed class ContractAnalyzer(
    IAnonymizer anonymizer,
    ContractAnalystAgent analyst,
    ClauseReviewerAgent reviewer,
    ReportVerifierAgent verifier)
{
    // Ile zapisów oceniamy naraz, żeby długa umowa nie przekroczyła limitów zapytań do modelu.
    private const int MaxParallelReviews = 4;

    public async IAsyncEnumerable<ContractAnalysisUpdate> AnalyzeAsync(
        string documentText,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ContractAnalysisProgress(ContractAnalysisStage.Anonymizing, 0, 1);
        var anonymized = anonymizer.Anonymize([documentText]);
        var text = anonymized.Texts[0];
        var clauses = ClauseSplitter.Split(text);

        yield return new ContractAnalysisProgress(ContractAnalysisStage.Analyzing, 0, 1);
        var overview = await analyst.AnalyzeAsync(text, cancellationToken);

        yield return new ContractAnalysisProgress(ContractAnalysisStage.Reviewing, 0, clauses.Count);
        using var limiter = new SemaphoreSlim(MaxParallelReviews);
        var reviews = clauses.Select(ReviewAsync).ToList();
        var completed = 0;

        await foreach (var _ in Task.WhenEach(reviews))
        {
            yield return new ContractAnalysisProgress(ContractAnalysisStage.Reviewing, ++completed, clauses.Count);
        }

        // Problemy w kolejności zapisów, niezależnie od tego, który recenzent skończył pierwszy.
        var risks = reviews.SelectMany(r => r.Result).ToList();

        yield return new ContractAnalysisProgress(ContractAnalysisStage.Verifying, 0, 1);
        var verified = await verifier.VerifyAsync(overview, risks, cancellationToken);

        yield return new ContractAnalysisCompleted(new ContractReport(
            overview.ContractType,
            overview.Summary,
            [.. verified.OrderByDescending(r => r.Level)],
            overview.MissingClauses,
            anonymized.Removed,
            text));

        async Task<IReadOnlyList<ContractRisk>> ReviewAsync(ContractClause clause)
        {
            await limiter.WaitAsync(cancellationToken);
            try
            {
                return await reviewer.ReviewAsync(overview, clause, cancellationToken);
            }
            finally
            {
                limiter.Release();
            }
        }
    }
}
