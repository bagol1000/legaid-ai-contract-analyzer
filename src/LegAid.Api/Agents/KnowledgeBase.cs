using System.Text.RegularExpressions;

namespace LegAid.Api.Agents;

public sealed record KnowledgeFragment(string Source, string Text);

// Przepisy z Knowledge/przepisy.txt, jeden artykuł = jeden fragment.
public sealed class KnowledgeBase
{
    private readonly List<(KnowledgeFragment Fragment, HashSet<string> Words)> _docs = [];

    public KnowledgeBase(string path)
    {
        var act = "";
        foreach (var line in File.ReadLines(path).Where(l => l.Trim() != ""))
        {
            var article = Regex.Match(line, @"^Art\. (\S+?)\.");
            if (line.StartsWith("Ustawa z dnia"))
            {
                // "... r. Kodeks cywilny" -> "KC", "... r. o ochronie praw lokatorów, ..." -> "Ustawa o ochronie praw lokatorów"
                var name = line[(line.IndexOf(" r. ") + 4)..].Split(',')[0];
                act = name.StartsWith("Kodeks ") ? "K" + char.ToUpper(name[7]) : "Ustawa " + name;
            }
            else if (article.Success)
                _docs.Add((new($"{act} art. {article.Groups[1].Value}", line), []));
            else
                _docs[^1] = (_docs[^1].Fragment with { Text = _docs[^1].Fragment.Text + "\n" + line }, []);
        }

        for (var i = 0; i < _docs.Count; i++)
            _docs[i] = (_docs[i].Fragment, Words(_docs[i].Fragment.Text).ToHashSet());
    }

    public int Count => _docs.Count;

    public List<KnowledgeFragment> Search(string query, int max)
    {
        // W umowie szukamy też osobno dla każdego paragrafu, żeby jeden temat nie zagłuszył reszty.
        var clauses = query.Split('§').Skip(1).ToList();
        if (clauses.Count < 2)
            return SearchText(query, max);

        return clauses.SelectMany(c => SearchText(c, 1))
            .Concat(SearchText(query, max))
            .Distinct().Take(max).ToList();
    }

    // Punkty za każde wspólne słowo, tym więcej, im rzadziej występuje w bazie.
    // Dzielimy przez długość przepisu, żeby długie artykuły nie wygrywały wszystkiego.
    private List<KnowledgeFragment> SearchText(string query, int max)
    {
        var words = Words(query).ToList();
        words.AddRange(words.Where(Synonyms.ContainsKey).SelectMany(w => Words(Synonyms[w])).ToList());

        var scored = _docs
            .Select(d => (d.Fragment, Score: words.Distinct().Where(d.Words.Contains)
                .Sum(w => Math.Log((double)_docs.Count / _docs.Count(x => x.Words.Contains(w)))) / Math.Sqrt(d.Words.Count)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        return scored.TakeWhile(x => x.Score >= scored[0].Score * 0.25)
            .Take(max).Select(x => x.Fragment).ToList();
    }

    private static IEnumerable<string> Words(string text) =>
        Regex.Matches(text.ToLower(), @"[\p{L}\p{N}¹²³]+")
            .Select(m => m.Value)
            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
            .Select(Stem);

    // Ucinamy końcówkę, żeby "kara" i "kary" albo "urlop" i "urlopu" dawały to samo słowo.
    private static string Stem(string word)
    {
        var ending = Endings.FirstOrDefault(e => word.EndsWith(e) && word.Length - e.Length >= 3) ?? "";
        var stem = word[..^ending.Length];
        return stem.Length > 7 ? stem[..7] : stem;
    }

    private static readonly string[] Endings = ["ami", "ach", "ego", "ych", "ej", "ie", "om", "ów", "ą", "ę", "a", "e", "i", "o", "u", "y"];

    // Słowa potoczne -> słowa z ustaw.
    private static readonly Dictionary<string, string> Synonyms = new()
    {
        [Stem("faktura")] = "sprzedaż przedawnienie",
        [Stem("spóźnienie")] = "opóźnienie",
        [Stem("mieszkanie")] = "lokal najem",
    };

    private static readonly HashSet<string> StopWords =
    [
        "czy", "jak", "jest", "jeżeli", "lub", "albo", "może", "nie", "oraz", "przez", "się", "tak", "tego",
        "być", "który", "każdy", "każdym", "gdy", "bez", "art", "ust", "dni", "dzień", "umowa", "umowy", "umowę"
    ];
}
