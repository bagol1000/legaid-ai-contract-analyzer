using System.Text.RegularExpressions;
using LegAid.Api.Model;

namespace LegAid.Api.Infrastructure.Knowledge;

// Format przepisy.txt: wiersz "Ustawa z dnia ..." zaczyna akt, wiersz "Art. X." zaczyna artykuł,
// pozostałe wiersze to dalsza część bieżącego artykułu. Bez dostępu do dysku, więc da się to testować na tekście.
public static class KnowledgeFileParser
{
    public static IReadOnlyList<KnowledgeFragment> Parse(IEnumerable<string> lines)
    {
        List<KnowledgeFragment> fragments = [];
        var act = "";

        foreach (var line in lines.Where(l => l.Trim() != ""))
        {
            var article = Regex.Match(line, @"^Art\. (\S+?)\.");
            if (line.StartsWith("Ustawa z dnia"))
            {
                // "... r. Kodeks cywilny" -> "KC", "... r. o ochronie praw lokatorów, ..." -> "Ustawa o ochronie praw lokatorów"
                var name = line[(line.IndexOf(" r. ") + 4)..].Split(',')[0];
                act = name.StartsWith("Kodeks ") ? "K" + char.ToUpper(name[7]) : "Ustawa " + name;
            }
            else if (article.Success)
                fragments.Add(new($"{act} art. {article.Groups[1].Value}", line));
            else
                fragments[^1] = fragments[^1] with { Text = fragments[^1].Text + "\n" + line };
        }

        return fragments;
    }
}
