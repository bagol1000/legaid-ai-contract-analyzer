using LegAid.Api.Agents;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LegAid.Api.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent").WithTags("Agent");

        group.MapPost("/ask", AskAsync)
            .WithName("AskLegalAgent")
            .WithSummary("Zadaj pytanie prawne albo wklej umowę lub pismo do analizy.");

        return app;
    }

    private static async Task<Results<Ok<AskResponse>, ValidationProblem>> AskAsync(
        AskRequest request,
        ILegalAgent agent,
        CancellationToken cancellationToken)
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

        var answer = await agent.AskAsync(request.Message, request.History, cancellationToken);

        return TypedResults.Ok(new AskResponse(answer.Text, answer.Model));
    }
}

/// <param name="Message">Pytanie prawne albo treść umowy lub pisma.</param>
/// <param name="History">Wcześniejsze wiadomości rozmowy, od najstarszej. Opcjonalne.</param>
public sealed record AskRequest(string? Message, List<ChatTurn>? History = null);

/// <param name="Answer">Odpowiedź agenta.</param>
/// <param name="Model">Model, który wygenerował odpowiedź.</param>
public sealed record AskResponse(string Answer, string? Model);
