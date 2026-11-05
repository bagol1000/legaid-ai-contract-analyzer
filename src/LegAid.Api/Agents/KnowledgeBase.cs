using System.Text.RegularExpressions;

namespace LegAid.Api.Agents;

public sealed record KnowledgeFragment(string Source, string Text);

// Przepisy z Knowledge/przepisy.txt, jeden artykuł = jeden fragment.
// Baza jest mała, więc model dostaje ją w całości, bez wyszukiwania.
public sealed class KnowledgeBase
{
    private readonly List<KnowledgeFragment> _fragments = [];

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
                _fragments.Add(new($"{act} art. {article.Groups[1].Value}", line));
            else
                _fragments[^1] = _fragments[^1] with { Text = _fragments[^1].Text + "\n" + line };
        }
    }

    public int Count => _fragments.Count;

    public IReadOnlyList<KnowledgeFragment> Fragments => _fragments;
}
