using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LegAid.Api.Abstractions;
using LegAid.Api.Logic;
using LegAid.Api.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

// Udawany model: zapamiętuje, co dostał, i odpowiada tekstem z funkcji (JSON przy odpowiedziach strukturalnych).
// Odpowiedź strumieniowa przychodzi w kawałkach po słowie.
internal sealed class FakeChatClient(Func<IReadOnlyList<ChatMessage>, string> respond) : IChatClient
{
    public ConcurrentQueue<IReadOnlyList<ChatMessage>> Calls { get; } = new();

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        Calls.Enqueue(list);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, respond(list))));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        Calls.Enqueue(list);
        foreach (var word in respond(list).Split(' '))
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

// Udawany anonimizator: zamienia tylko "Jan Kowalski", żeby testy logiki nie zależały od reguł anonimizacji.
internal sealed class FakeAnonymizer : IAnonymizer
{
    public AnonymizationResult Anonymize(IReadOnlyList<string> texts)
    {
        var found = texts.Any(t => t.Contains("Jan Kowalski"));
        return new([.. texts.Select(t => t.Replace("Jan Kowalski", "[OSOBA_1]"))],
            found ? new Dictionary<PersonalDataKind, int> { [PersonalDataKind.Person] = 1 } : []);
    }
}

internal sealed class FakeKnowledgeSource : IKnowledgeSource
{
    public IReadOnlyList<KnowledgeFragment> GetAll() =>
        [new("KC art. 483", "Art. 483. § 1. Można zastrzec w umowie, że naprawienie szkody ... nastąpi przez zapłatę kary umownej.")];
}

internal static class TestAgents
{
    public static AgentMessages Messages(bool knowledgeEnabled = true) =>
        new(new FakeKnowledgeSource(), Options.Create(new KnowledgeOptions { Enabled = knowledgeEnabled }));
}
