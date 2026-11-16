using System.Net.ServerSentEvents;
using LegAid.Api.Abstractions;
using LegAid.Api.Logic.ContractAnalysis;
using LegAid.Api.Model;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LegAid.Api.Presentation.Endpoints;

public static class ContractEndpoints
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/contract/analyze", AnalyzeAsync)
            .WithTags("Umowy")
            .WithName("AnalyzeContract")
            .WithSummary("Wgraj umowę (PDF lub DOCX, pole 'file'). Odpowiedź jako Server-Sent Events: "
                + "zdarzenia 'progress', na końcu 'report' z raportem ryzyk albo 'error'.")
            // API bez ciasteczek i logowania, więc ochrona przed CSRF nie jest potrzebna.
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Results<ServerSentEventsResult<string>, ValidationProblem>> AnalyzeAsync(
        IFormFile? file,
        IDocumentTextExtractor extractor,
        ContractAnalyzer analyzer,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file?.FileName ?? "").ToLowerInvariant();

        if (file is null || file.Length == 0)
            return FileProblem("Wybierz plik PDF lub DOCX z umową.");
        if (!extractor.SupportedExtensions.Contains(extension))
            return FileProblem("Obsługiwane są tylko pliki PDF i DOCX.");
        if (file.Length > MaxFileSize)
            return FileProblem("Plik może mieć najwyżej 10 MB.");

        string text;
        try
        {
            await using var stream = file.OpenReadStream();
            text = await extractor.ExtractTextAsync(stream, extension, cancellationToken);
        }
        catch (InvalidDataException)
        {
            return FileProblem("Nie udało się odczytać pliku.");
        }

        if (string.IsNullOrWhiteSpace(text))
            return FileProblem("W pliku nie ma tekstu. Jeśli to skan, potrzebna jest wersja z tekstem.");

        var events = ServerSentEventStream.Create(
            analyzer.AnalyzeAsync(text, cancellationToken),
            ToEvent,
            lastEvent: null,
            loggerFactory.CreateLogger(typeof(ContractEndpoints)),
            cancellationToken);

        return TypedResults.ServerSentEvents(events);
    }

    private static SseItem<string> ToEvent(ContractAnalysisUpdate update) => update switch
    {
        ContractAnalysisProgress progress => ServerSentEventStream.Event("progress", AnalysisProgressDto.From(progress)),
        ContractAnalysisCompleted completed => ServerSentEventStream.Event("report", ContractReportDto.From(completed.Report)),
        _ => throw new ArgumentOutOfRangeException(nameof(update))
    };

    private static ValidationProblem FileProblem(string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [message] });
}
