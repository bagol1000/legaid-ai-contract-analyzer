using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace LegAid.Api.Presentation.Endpoints;

// Zamienia strumień wyników na zdarzenia Server-Sent Events. Dane każdego zdarzenia to JSON,
// więc znaki nowej linii w tekście nie psują formatu SSE.
internal static class ServerSentEventStream
{
    public static SseItem<string> Event(string type, object data) =>
        new(JsonSerializer.Serialize(data, JsonSerializerOptions.Web), type);

    // Status 200 idzie przed pierwszym zdarzeniem, dlatego błąd w trakcie przychodzi jako zdarzenie 'error'.
    public static async IAsyncEnumerable<SseItem<string>> Create<T>(
        IAsyncEnumerable<T> source,
        Func<T, SseItem<string>> toEvent,
        SseItem<string>? lastEvent,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
        var failed = false;

        while (true)
        {
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Błąd podczas strumieniowania odpowiedzi");
                failed = true;
                break;
            }

            yield return toEvent(enumerator.Current);
        }

        if (failed)
        {
            yield return Event("error", "Nie udało się dokończyć odpowiedzi.");
        }
        else if (lastEvent is { } last)
        {
            yield return last;
        }
    }
}
