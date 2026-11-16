using LegAid.Api.Abstractions;
using LegAid.Api.Model;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LegAid.Api.Presentation.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent").WithTags("Agent");

        group.MapPost("/ask", AskAsync)
            .WithName("AskLegalAgent")
            .WithSummary("Zadaj pytanie prawne albo wklej umowę lub pismo do analizy.");

        group.MapPost("/ask/stream", AskStream)
            .WithName("AskLegalAgentStream")
            .WithSummary("Jak /ask, ale odpowiedź przychodzi na bieżąco jako Server-Sent Events: "
                + "zdarzenia 'delta' z kolejnymi kawałkami tekstu, na końcu 'done' albo 'error'.");

        return app;
    }

    private static async Task<Results<Ok<AskResponse>, ValidationProblem>> AskAsync(
        AskRequest request,
        ILegalAgent agent,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var answer = await agent.AskAsync(request.Message!, ToHistory(request), cancellationToken);

        return TypedResults.Ok(new AskResponse(answer.Text, answer.Model));
    }

    private static Results<ServerSentEventsResult<string>, ValidationProblem> AskStream(
        AskRequest request,
        ILegalAgent agent,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var events = ServerSentEventStream.Create(
            agent.AskStreamingAsync(request.Message!, ToHistory(request), cancellationToken),
            chunk => ServerSentEventStream.Event("delta", chunk),
            ServerSentEventStream.Event("done", new { }),
            loggerFactory.CreateLogger(typeof(AgentEndpoints)),
            cancellationToken);

        return TypedResults.ServerSentEvents(events);
    }

    private static ValidationProblem? Validate(AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["message"] = ["Pole 'message' nie może być puste."]
            });
        }

        // Tylko te dwie role: klient nie może dopisać własnej instrukcji systemowej.
        if (request.History?.Any(t => t is not { Role: "user" or "assistant", Text.Length: > 0 }) == true)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["history"] = ["Każda wiadomość w 'history' musi mieć rolę 'user' lub 'assistant' i niepustą treść."]
            });
        }

        return null;
    }

    private static List<ChatTurn>? ToHistory(AskRequest request) =>
        request.History?
            .Select(t => new ChatTurn(t.Role == "assistant" ? ChatTurnRole.Assistant : ChatTurnRole.User, t.Text))
            .ToList();
}
