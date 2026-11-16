using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

// Ewaluacja LegAId na działającym serwerze: pytania z demo i przykładowe umowy z samples/.
// Użycie: dotnet run --project tests/LegAid.Eval [-- --url http://localhost:5080]

Console.OutputEncoding = Encoding.UTF8;

var url = args.SkipWhile(a => a != "--url").Skip(1).FirstOrDefault() ?? "http://localhost:5080";
var cases = JsonSerializer.Deserialize<EvalCases>(
    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cases.json")),
    new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var repoRoot = FindRepoRoot();

using var http = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(180) };

try
{
    await http.GetAsync("/");
}
catch (HttpRequestException)
{
    Console.WriteLine($"Serwer pod {url} nie odpowiada. Uruchom go w drugim oknie: cd src/LegAid.Api; dotnet run");
    return 1;
}

List<CaseResult> results = [];

foreach (var chat in cases.Chat)
{
    results.Add(await RunAsync(chat.Name, chat.Expected.Count, async () =>
    {
        var response = await http.PostAsJsonAsync("/api/agent/ask", new { message = chat.Message });
        response.EnsureSuccessStatusCode();
        var answer = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("answer").GetString()!;

        var missing = Checks.MissingLabels(answer, chat.Expected);
        List<string> problems = [.. missing.Select(l => $"brak: {l}")];
        if (chat.Phrase is not null && !answer.Contains(chat.Phrase, StringComparison.OrdinalIgnoreCase))
            problems.Add($"brak frazy „{chat.Phrase}”");
        return (chat.Expected.Count - missing.Count, problems);
    }));
}

foreach (var document in cases.Documents)
{
    results.Add(await RunAsync(document.Name, document.Expected.Count, async () =>
    {
        var path = Path.Combine(repoRoot, document.File);
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(File.OpenRead(path));
        file.Headers.ContentType = new MediaTypeHeaderValue(path.EndsWith(".pdf")
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        form.Add(file, "file", Path.GetFileName(path));

        using var response = await http.PostAsync("/api/contract/analyze", form);
        response.EnsureSuccessStatusCode();
        var report = await Checks.ReadReportAsync(await response.Content.ReadAsStreamAsync(), CancellationToken.None);

        var legalBasis = string.Join("\n", Checks.LegalBasis(report));
        var missing = Checks.MissingLabels(legalBasis, document.Expected);
        var reportText = Checks.AllStrings(report);

        List<string> problems = [.. missing.Select(l => $"brak: {l}")];
        problems.AddRange(document.MustNotContain
            .Where(value => reportText.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Select(value => $"wyciek: {value}"));
        return (document.Expected.Count - missing.Count, problems);
    }));
}

var passed = results.Count(r => r.Passed);
var hit = results.Sum(r => r.Hit);
var expected = results.Sum(r => r.Expected);
Console.WriteLine();
Console.WriteLine($"Zaliczone przypadki: {passed}/{results.Count}, trafione etykiety: {hit}/{expected} ({(expected == 0 ? 100 : 100.0 * hit / expected):0}%)");

return passed == results.Count ? 0 : 1;

// Uruchamia jeden przypadek, mierzy czas i wypisuje wynik w jednej linii.
static async Task<CaseResult> RunAsync(string name, int expected, Func<Task<(int Hit, List<string> Problems)>> run)
{
    var stopwatch = Stopwatch.StartNew();
    CaseResult result;
    try
    {
        var (hit, problems) = await run();
        result = new(problems.Count == 0, hit, expected, problems);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or IOException)
    {
        result = new(false, 0, expected, [$"błąd: {ex.Message}"]);
    }

    var details = result.Problems.Count > 0 ? "  " + string.Join("; ", result.Problems) : "";
    Console.WriteLine($"{(result.Passed ? "✓" : "✗")} {name,-26} {stopwatch.Elapsed.TotalSeconds,5:0.0} s  {result.Hit}/{result.Expected}{details}");
    return result;
}

// Korzeń repo (z LegAid.slnx) nad katalogiem programu albo nad katalogiem bieżącym.
static string FindRepoRoot()
{
    foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LegAid.slnx")))
                return dir.FullName;
        }
    }
    throw new InvalidOperationException("Nie znaleziono LegAid.slnx nad katalogiem programu ani bieżącym.");
}

record EvalCases(List<ChatCase> Chat, List<DocumentCase> Documents);

record ChatCase(string Name, string Message, List<string> Expected, string? Phrase = null);

record DocumentCase(string Name, string File, List<string> Expected, List<string> MustNotContain);

record CaseResult(bool Passed, int Hit, int Expected, List<string> Problems);
