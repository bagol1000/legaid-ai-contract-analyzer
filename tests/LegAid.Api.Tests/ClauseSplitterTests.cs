using LegAid.Api.Logic.ContractAnalysis;

public class ClauseSplitterTests
{
    [Fact]
    public void SplitsByParagraphsAndSkipsPreamble()
    {
        var clauses = ClauseSplitter.Split("""
            UMOWA NAJMU
            zawarta pomiędzy [OSOBA_1] a [OSOBA_2]
            § 1. Przedmiotem najmu jest lokal.
            § 2. Czynsz wynosi 2800 zł
            i jest płatny zgodnie z § 1 do 10. dnia miesiąca.
            §3 Kaucja wynosi 5600 zł.
            """);

        Assert.Equal(["§ 1", "§ 2", "§ 3"], clauses.Select(c => c.Label));
        Assert.Equal("§ 2. Czynsz wynosi 2800 zł\ni jest płatny zgodnie z § 1 do 10. dnia miesiąca.", clauses[1].Text);
    }

    [Fact]
    public void WithoutParagraphsSplitsIntoFragmentsOnLineBoundaries()
    {
        var line = new string('a', 600);
        var clauses = ClauseSplitter.Split(string.Join("\n", Enumerable.Repeat(line, 5)));

        Assert.Equal(["Fragment 1", "Fragment 2", "Fragment 3"], clauses.Select(c => c.Label));
        Assert.All(clauses, c => Assert.True(c.Text.Length <= 1500));
        Assert.Equal(5, clauses.Sum(c => c.Text.Split('\n').Length));
    }
}
