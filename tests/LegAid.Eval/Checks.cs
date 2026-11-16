using System.Text.Json;
using System.Text.RegularExpressions;

// Sprawdzanie odpowiedzi: dopasowanie etykiet przepisów i odczyt strumienia SSE z analizy umowy.
static class Checks
{
    // Po etykiecie nie może stać cyfra ani indeks górny, więc "KC art. 48" nie pasuje do "KC art. 483".
    // Przed "art." dopuszczamy przecinek, bo model pisze też "Ustawa o ochronie praw lokatorów, art. 6".
    public static bool ContainsLabel(string text, string label) =>
        Regex.IsMatch(text, Regex.Escape(label).Replace(@"\ art\.", @",?\ art\.") + @"(?![\d¹²³⁴⁵⁶⁷⁸⁹⁰])", RegexOptions.IgnoreCase);

    public static List<string> MissingLabels(string text, IEnumerable<string> expected) =>
        expected.Where(label => !ContainsLabel(text, label)).ToList();

    // Zwraca dane zdarzenia "report"; zdarzenie "error" kończy się wyjątkiem, "progress" jest pomijane.
    public static async Task<JsonElement> ReadReportAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        string type = "message", data = "";

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("event:"))
                type = line[6..].Trim();
            else if (line.StartsWith("data:"))
                data += line[5..].TrimStart(' ');
            else if (line == "")
            {
                if (type == "report")
                    return JsonDocument.Parse(data).RootElement.Clone();
                if (type == "error")
                    throw new InvalidOperationException(JsonSerializer.Deserialize<string>(data));
                (type, data) = ("message", "");
            }
        }

        throw new InvalidOperationException("Strumień skończył się bez raportu.");
    }

    // Wszystkie teksty z JSON-a, już bez escape'owania \uXXXX, żeby szukać w nich wycieku danych osobowych.
    public static string AllStrings(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Array => string.Join("\n", element.EnumerateArray().Select(AllStrings)),
        JsonValueKind.Object => string.Join("\n", element.EnumerateObject().Select(p => p.Name + "\n" + AllStrings(p.Value))),
        _ => element.ToString()
    };

    public static List<string> LegalBasis(JsonElement report) =>
        report.GetProperty("risks").EnumerateArray()
            .SelectMany(risk => risk.GetProperty("legalBasis").EnumerateArray())
            .Select(label => label.GetString()!)
            .ToList();
}
