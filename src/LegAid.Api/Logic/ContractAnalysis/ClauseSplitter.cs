using System.Text.RegularExpressions;
using LegAid.Api.Model;

namespace LegAid.Api.Logic.ContractAnalysis;

// Dzieli umowę na paragrafy według wierszy zaczynających się od "§ N". Wstęp przed pierwszym paragrafem
// (tytuł, strony) pomijamy, bo analityk i tak widzi całą umowę. Bez paragrafów tniemy na fragmenty po wierszach.
public static partial class ClauseSplitter
{
    private const int MaxFragmentLength = 1500;

    public static IReadOnlyList<ContractClause> Split(string text)
    {
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        List<ContractClause> clauses = [];

        foreach (var line in lines)
        {
            var heading = ParagraphHeading().Match(line);
            if (heading.Success)
                clauses.Add(new($"§ {heading.Groups[1].Value}", line));
            else if (clauses.Count > 0)
                clauses[^1] = clauses[^1] with { Text = clauses[^1].Text + "\n" + line };
        }

        return clauses.Count > 0 ? clauses : SplitIntoFragments(lines);
    }

    private static List<ContractClause> SplitIntoFragments(List<string> lines)
    {
        List<ContractClause> fragments = [];
        var current = "";

        foreach (var line in lines)
        {
            if (current.Length > 0 && current.Length + line.Length > MaxFragmentLength)
            {
                fragments.Add(new($"Fragment {fragments.Count + 1}", current));
                current = "";
            }
            current = current.Length == 0 ? line : current + "\n" + line;
        }

        if (current.Length > 0)
            fragments.Add(new($"Fragment {fragments.Count + 1}", current));

        return fragments;
    }

    [GeneratedRegex(@"^§\s*(\d+[a-z]?)\b")]
    private static partial Regex ParagraphHeading();
}
