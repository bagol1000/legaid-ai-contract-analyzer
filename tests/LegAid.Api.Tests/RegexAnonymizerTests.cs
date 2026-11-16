using LegAid.Api.Infrastructure.Anonymization;
using LegAid.Api.Model;

public class RegexAnonymizerTests
{
    private readonly RegexAnonymizer _anonymizer = new();

    private string Anonymize(string text) => _anonymizer.Anonymize([text]).Texts[0];

    [Theory]
    [InlineData("PESEL 76031206830.", "PESEL [PESEL_1].")]
    [InlineData("NIP 526-000-12-46", "NIP [NIP_1]")]
    [InlineData("NIP 526-00-01-246", "NIP [NIP_1]")]
    [InlineData("NIP PL5260001246", "NIP [NIP_1]")]
    [InlineData("REGON 123456785", "REGON [REGON_1]")]
    [InlineData("REGON 12345678512347", "REGON [REGON_1]")]
    [InlineData("dowód osobisty ABS012345", "dowód osobisty [DOWOD_1]")]
    [InlineData("rachunek PL61 1090 1014 0000 0712 1981 2874.", "rachunek [KONTO_1].")]
    [InlineData("rachunek 61109010140000071219812874", "rachunek [KONTO_1]")]
    [InlineData("e-mail: k.zielinska@example.com", "e-mail: [EMAIL_1]")]
    [InlineData("tel. +48 601 234 567", "tel. [TELEFON_1]")]
    [InlineData("tel. 601234567", "tel. [TELEFON_1]")]
    [InlineData("telefon 601-234-567", "telefon [TELEFON_1]")]
    [InlineData("biuro: 22 123 45 67", "biuro: [TELEFON_1]")]
    [InlineData("zam. ul. Kwiatowa 15/4, 00-950 Warszawa", "zam. [ADRES_1]")]
    [InlineData("lokal przy ul. Słoneczna 8 m. 12", "lokal przy [ADRES_1]")]
    [InlineData("al. Jana Pawła II 3 lok. 5", "[ADRES_1]")]
    [InlineData("ul. 3 Maja 10", "[ADRES_1]")]
    [InlineData("adres: 30-001 Kraków", "adres: [ADRES_1]")]
    [InlineData("ur. 12.03.1985 r.", "ur. [DATA_URODZENIA_1] r.")]
    [InlineData("urodzona 1985-03-12", "urodzona [DATA_URODZENIA_1]")]
    [InlineData("urodzonym dnia 12 marca 1985", "urodzonym dnia [DATA_URODZENIA_1]")]
    [InlineData("Tomasz Wiśniewski", "[OSOBA_1]")]
    [InlineData("z Tomaszem Wiśniewskim", "z [OSOBA_1]")]
    [InlineData("z Katarzyną Zielińską", "z [OSOBA_1]")]
    [InlineData("Pawła Nowaka", "[OSOBA_1]")]
    [InlineData("Anna Maria Nowak-Kowalska", "[OSOBA_1]")]
    [InlineData("Pani Zielińska", "Pani [OSOBA_1]")]
    [InlineData("przez pana Kowalskiego", "przez pana [OSOBA_1]")]
    public void ReplacesPersonalData(string input, string expected) => Assert.Equal(expected, Anonymize(input));

    [Theory]
    [InlineData("PESEL 76031206831")] // zła suma kontrolna
    [InlineData("NIP 5260001247")]
    [InlineData("dowód ABS012346")]
    [InlineData("rachunek PL62 1090 1014 0000 0712 1981 2874")]
    [InlineData("Czynsz wynosi 2800 zł, a kara umowna 1000 zł.")]
    [InlineData("Kwota 100 000 000 zł.")]
    [InlineData("§ 5. Zgodnie z art. 483 Kodeks cywilny przewiduje karę umowną.")]
    [InlineData("Umowa zawarta dnia 1 marca 2024 r. oraz 12.03.2024 r.")]
    [InlineData("Strony Umowy ustalają, że Najemca płaci w maju i czerwcu.")]
    [InlineData("Tomasz zapłaci czynsz.")] // samo imię bez nazwiska
    [InlineData("Wynajmujący Tomasz")]
    public void LeavesTextWithoutPersonalData(string input) => Assert.Equal(input, Anonymize(input));

    [Fact]
    public void SameValueGetsSamePlaceholderAcrossTexts()
    {
        var result = _anonymizer.Anonymize(["Najemca: Tomasz Wiśniewski, PESEL 76031206830.", "Czy Tomasz Wiśniewski (76031206830) musi płacić? Anna Nowak też."]);

        Assert.Equal("Najemca: [OSOBA_1], PESEL [PESEL_1].", result.Texts[0]);
        Assert.Equal("Czy [OSOBA_1] ([PESEL_1]) musi płacić? [OSOBA_2] też.", result.Texts[1]);
        Assert.Equal(2, result.Removed[PersonalDataKind.Person]);
        Assert.Equal(1, result.Removed[PersonalDataKind.Pesel]);
        Assert.Equal(2, result.Removed.Count);
    }

    [Fact]
    public void ContinuesNumberingAfterExistingPlaceholders()
    {
        var result = _anonymizer.Anonymize(["Umowa między [OSOBA_1] a [OSOBA_2].", "A co, jeśli Anna Nowak nie zapłaci?"]);

        Assert.Equal("Umowa między [OSOBA_1] a [OSOBA_2].", result.Texts[0]);
        Assert.Equal("A co, jeśli [OSOBA_3] nie zapłaci?", result.Texts[1]);
        Assert.Equal(1, result.Removed[PersonalDataKind.Person]);
    }

    [Fact]
    public void TextWithoutPersonalDataIsUnchanged()
    {
        var result = _anonymizer.Anonymize(["Czy wynajmujący może zatrzymać kaucję po wyprowadzce?"]);

        Assert.Equal("Czy wynajmujący może zatrzymać kaucję po wyprowadzce?", result.Texts[0]);
        Assert.Empty(result.Removed);
    }

    [Fact]
    public void AnonymizesSampleContract()
    {
        const string contract = """
            Wynajmujący: Tomasz Wiśniewski, PESEL 76031206830, zam. ul. Kwiatowa 15/4, 00-950 Warszawa
            Najemca: Katarzyna Zielińska, PESEL 92051407424, dowód osobisty ABS012345, tel. +48 601 234 567, e-mail k.zielinska@example.com
            Przedmiotem najmu jest lokal przy ul. Słoneczna 8 m. 12, 02-495 Warszawa. Czynsz wynosi 2800 zł miesięcznie i jest płatny na rachunek PL61 1090 1014 0000 0712 1981 2874.
            § 5. Za każdy dzień opóźnienia w zapłacie czynszu Najemca zapłaci karę umowną w wysokości 1000 zł.
            """;

        var result = _anonymizer.Anonymize([contract]);
        var text = result.Texts[0];

        foreach (var personal in new[] { "Wiśniewsk", "Zielińsk", "76031206830", "92051407424", "ABS012345", "Kwiatowa", "Słoneczna", "601 234 567", "k.zielinska", "1090 1014" })
        {
            Assert.DoesNotContain(personal, text);
        }

        foreach (var kept in new[] { "§ 5", "1000 zł", "2800 zł", "Wynajmujący:", "Najemca:" })
        {
            Assert.Contains(kept, text);
        }

        Assert.Equal(2, result.Removed[PersonalDataKind.Person]);
        Assert.Equal(2, result.Removed[PersonalDataKind.Pesel]);
        Assert.Equal(2, result.Removed[PersonalDataKind.Address]);
        Assert.Equal(1, result.Removed[PersonalDataKind.IdCard]);
        Assert.Equal(1, result.Removed[PersonalDataKind.Phone]);
        Assert.Equal(1, result.Removed[PersonalDataKind.Email]);
        Assert.Equal(1, result.Removed[PersonalDataKind.BankAccount]);
    }
}
