using System.Text.RegularExpressions;
using LegAid.Api.Abstractions;
using LegAid.Api.Model;

namespace LegAid.Api.Infrastructure.Anonymization;

// Anonimizacja regułami, bez wysyłania tekstu poza serwer. Numery identyfikacyjne są sprawdzane sumą kontrolną,
// a osoby rozpoznawane po popularnym imieniu (także odmienionym) albo po zwrocie "Pan/Pani" przed nazwiskiem.
public sealed partial class RegexAnonymizer : IAnonymizer
{
    private static readonly Dictionary<PersonalDataKind, string> Tags = new()
    {
        [PersonalDataKind.Person] = "OSOBA",
        [PersonalDataKind.Pesel] = "PESEL",
        [PersonalDataKind.Nip] = "NIP",
        [PersonalDataKind.Regon] = "REGON",
        [PersonalDataKind.IdCard] = "DOWOD",
        [PersonalDataKind.BankAccount] = "KONTO",
        [PersonalDataKind.Email] = "EMAIL",
        [PersonalDataKind.Phone] = "TELEFON",
        [PersonalDataKind.Address] = "ADRES",
        [PersonalDataKind.BirthDate] = "DATA_URODZENIA"
    };

    private static readonly string[] Titles = ["pan", "pani", "pana", "panu", "panem", "panią"];

    // Słowa z wielkiej litery, które w umowach stoją obok imion, a nie są nazwiskami.
    private static readonly string[] NonSurnameStems =
    [
        "Najem", "Wynajm", "Stron", "Umow", "Klient", "Zlecen", "Pracodaw", "Pracowni", "Sprzedaw", "Kupując", "Nabyw",
        "Dłużni", "Wierzyciel", "Właściciel", "Lokator", "Konsument", "Przedsiębior", "Sąd", "Kodeks", "Ustaw", "Spółk",
        "Bank", "Urząd", "Rzecz"
    ];

    public AnonymizationResult Anonymize(IReadOnlyList<string> texts)
    {
        var session = new Session(texts);
        var anonymized = texts.Select(session.Anonymize).ToList();
        return new AnonymizationResult(anonymized, session.Removed());
    }

    // Stan jednego wywołania: wspólne znaczniki dla wszystkich tekstów.
    private sealed class Session
    {
        private readonly Dictionary<PersonalDataKind, Dictionary<string, int>> _values = [];
        private readonly Dictionary<PersonalDataKind, int> _last = [];

        public Session(IEnumerable<string> texts)
        {
            // Numeracja zaczyna się za znacznikami, które już są w tekstach, np. z wcześniejszej analizy.
            foreach (Match m in texts.SelectMany(t => ExistingPlaceholderRegex().Matches(t)))
            {
                var kind = Tags.First(t => t.Value == m.Groups["tag"].Value).Key;
                _last[kind] = Math.Max(_last.GetValueOrDefault(kind), int.Parse(m.Groups["n"].Value));
            }
        }

        public string Anonymize(string text)
        {
            text = Replace(text, EmailRegex(), PersonalDataKind.Email, m => m.Value.ToLowerInvariant());
            text = Replace(text, BankAccountRegex(), PersonalDataKind.BankAccount, m => Digits(m.Value) is var d && IsValidIban(d) ? d : null);
            text = Replace(text, StreetAddressRegex(), PersonalDataKind.Address, m => Normalize(m.Value));
            text = Replace(text, PostalCodeRegex(), PersonalDataKind.Address, m => Normalize(m.Value));
            text = Replace(text, BirthDateRegex(), PersonalDataKind.BirthDate, m => Normalize(m.Groups["v"].Value));
            text = Replace(text, IdCardRegex(), PersonalDataKind.IdCard, m => m.Value.Replace(" ", "") is var id && IsValidIdCard(id) ? id : null);
            text = Replace(text, NipRegex(), PersonalDataKind.Nip, m => Digits(m.Value) is var d && IsValidNip(d) ? d : null);
            text = Replace(text, Regon14Regex(), PersonalDataKind.Regon, m => IsValidRegon(m.Value) ? m.Value : null);
            text = Replace(text, PeselRegex(), PersonalDataKind.Pesel, m => IsValidPesel(m.Value) ? m.Value : null);
            text = Replace(text, PhoneRegex(), PersonalDataKind.Phone, m => Digits(m.Groups["v"].Value)[^9..]);
            text = Replace(text, Regon9Regex(), PersonalDataKind.Regon, m => IsValidRegon(m.Value) ? m.Value : null);
            return ReplacePersons(text);
        }

        public IReadOnlyDictionary<PersonalDataKind, int> Removed() =>
            _values.Where(v => v.Value.Count > 0).ToDictionary(v => v.Key, v => v.Value.Count);

        // key zwraca znormalizowaną wartość albo null, gdy trafienie nie jest daną osobową (np. zła suma kontrolna).
        // Gdy wzorzec ma grupę "v", zastępowana jest tylko ona, a reszta trafienia (np. "ur.") zostaje.
        private string Replace(string text, Regex regex, PersonalDataKind kind, Func<Match, string?> key) =>
            regex.Replace(text, m =>
            {
                if (key(m) is not { } value)
                {
                    return m.Value;
                }

                var v = m.Groups["v"];
                var span = v.Success ? v : (Group)m;
                return m.Value[..(span.Index - m.Index)] + Placeholder(kind, value) + m.Value[(span.Index - m.Index + span.Length)..];
            });

        // Imię (także odmienione) + nazwisko, opcjonalnie z drugim imieniem, albo "Pan/Pani" + jedno lub dwa słowa.
        private string ReplacePersons(string text)
        {
            var words = WordRegex().Matches(text);
            var result = new System.Text.StringBuilder();
            var copied = 0;

            for (var i = 0; i < words.Count; i++)
            {
                int start, end;
                if (Titles.Contains(words[i].Value.ToLowerInvariant()))
                {
                    if (!IsNamePart(text, words, i + 1, i))
                    {
                        continue;
                    }

                    start = end = i + 1;
                    if (IsNamePart(text, words, end + 1, end))
                    {
                        end++;
                    }
                }
                else if (PolishFirstNames.IsFirstName(words[i].Value) && IsNamePart(text, words, i + 1, i))
                {
                    start = i;
                    end = i + 1;
                    if (PolishFirstNames.IsFirstName(words[end].Value) && IsNamePart(text, words, end + 1, end))
                    {
                        end++; // drugie imię przed nazwiskiem
                    }
                }
                else
                {
                    continue; // samo imię bez nazwiska zostaje
                }

                var from = words[start].Index;
                var to = words[end].Index + words[end].Length;
                result.Append(text, copied, from - copied).Append(Placeholder(PersonalDataKind.Person, Normalize(text[from..to])));
                copied = to;
                i = end;
            }

            return result.Append(text, copied, text.Length - copied).ToString();
        }

        // Słowo pod indeksem j stoi tuż po słowie prev (tylko spacje między nimi) i może być częścią imienia i nazwiska.
        private static bool IsNamePart(string text, MatchCollection words, int j, int prev)
        {
            if (j >= words.Count)
            {
                return false;
            }

            var gap = text[(words[prev].Index + words[prev].Length)..words[j].Index];
            var word = words[j].Value;
            return gap.Length > 0 && gap.All(c => c is ' ' or '\t') && IsCapitalized(word)
                && !Titles.Contains(word.ToLowerInvariant())
                && !NonSurnameStems.Any(s => word.StartsWith(s, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCapitalized(string word) =>
            word.Split('-').All(p => p.Length >= 2 && char.IsUpper(p[0]) && p.Skip(1).All(char.IsLower));

        private string Placeholder(PersonalDataKind kind, string value)
        {
            var values = _values.TryGetValue(kind, out var v) ? v : _values[kind] = [];
            if (!values.TryGetValue(value, out var n))
            {
                n = values[value] = _last[kind] = _last.GetValueOrDefault(kind) + 1;
            }

            return $"[{Tags[kind]}_{n}]";
        }
    }

    private static string Digits(string value) => new(value.Where(char.IsAsciiDigit).ToArray());

    private static string Normalize(string value) => WhitespaceRegex().Replace(value.Trim(), " ");

    private static bool IsValidPesel(string d) =>
        (10 - Weighted(d, [1, 3, 7, 9, 1, 3, 7, 9, 1, 3]) % 10) % 10 == d[10] - '0';

    private static bool IsValidNip(string d) =>
        Weighted(d, [6, 5, 7, 2, 3, 4, 5, 6, 7]) % 11 is var c && c != 10 && c == d[9] - '0';

    private static bool IsValidRegon(string d)
    {
        int[] weights = d.Length == 9 ? [8, 9, 2, 3, 4, 5, 6, 7] : [2, 4, 8, 5, 0, 9, 7, 3, 6, 1, 2, 4, 8];
        return Weighted(d, weights) % 11 % 10 == d[^1] - '0';
    }

    // Seria: litery A=10..Z=35, cyfra kontrolna na 4. pozycji.
    private static bool IsValidIdCard(string id)
    {
        int Value(char c) => char.IsAsciiDigit(c) ? c - '0' : c - 'A' + 10;
        int[] weights = [7, 3, 1, 0, 7, 3, 1, 7, 3];
        return id.Select((c, i) => Value(c) * weights[i]).Sum() % 10 == id[3] - '0';
    }

    // IBAN PL: 2 cyfry kontrolne + 24 cyfry numeru, kontrola mod 97 po przeniesieniu "PL" i cyfr kontrolnych na koniec.
    private static bool IsValidIban(string d) =>
        (d[2..] + "2521" + d[..2]).Aggregate(0, (mod, c) => (mod * 10 + (c - '0')) % 97) == 1;

    private static int Weighted(string digits, int[] weights) => weights.Select((w, i) => w * (digits[i] - '0')).Sum();

    [GeneratedRegex(@"\[(?<tag>OSOBA|PESEL|NIP|REGON|DOWOD|KONTO|EMAIL|TELEFON|ADRES|DATA_URODZENIA)_(?<n>\d+)\]")]
    private static partial Regex ExistingPlaceholderRegex();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:PL[ ]?)?\d{2}(?:[ ]?\d{4}){6}\b")]
    private static partial Regex BankAccountRegex();

    // ul./al./pl./os. + nazwa (np. "Jana Pawła II", "3 Maja") + numer domu, mieszkania lub lokalu, opcjonalnie kod i miasto.
    [GeneratedRegex(@"(?<![\p{L}])(?i:ul|al|pl|os)\.[ \t]*(?:(?:\p{Lu}[\p{L}-]*\.?|\d{1,2})[ \t]+){1,5}\d+[A-Za-z]?(?:[ \t]*/[ \t]*\d+[A-Za-z]?)?(?:[ \t]*(?:m\.|lok\.)[ \t]*\d+[A-Za-z]?)?(?:,?[ \t]*\d{2}-\d{3}[ \t]+\p{Lu}\p{Ll}+(?:-\p{Lu}\p{Ll}+)?)?")]
    private static partial Regex StreetAddressRegex();

    [GeneratedRegex(@"\b\d{2}-\d{3}[ \t]+\p{Lu}\p{Ll}+(?:-\p{Lu}\p{Ll}+)?")]
    private static partial Regex PostalCodeRegex();

    [GeneratedRegex(@"\b(?:ur\.|urodzon(?:y|a|ym|ą|ej))[ \t]*(?:dnia[ \t]+)?(?<v>\d{1,2}\.\d{1,2}\.\d{4}|\d{4}-\d{2}-\d{2}|\d{1,2}[ \t]+(?:stycznia|lutego|marca|kwietnia|maja|czerwca|lipca|sierpnia|września|października|listopada|grudnia)[ \t]+\d{4})")]
    private static partial Regex BirthDateRegex();

    [GeneratedRegex(@"\b[A-Z]{3}[ ]?\d{6}\b")]
    private static partial Regex IdCardRegex();

    [GeneratedRegex(@"\b(?:PL)?(?:\d{3}-\d{3}-\d{2}-\d{2}|\d{3}-\d{2}-\d{2}-\d{3}|\d{10})\b")]
    private static partial Regex NipRegex();

    [GeneratedRegex(@"\b\d{14}\b")]
    private static partial Regex Regon14Regex();

    [GeneratedRegex(@"\b\d{11}\b")]
    private static partial Regex PeselRegex();

    [GeneratedRegex(@"\b\d{9}\b")]
    private static partial Regex Regon9Regex();

    // Komórkowy 3-3-3, stacjonarny 2-3-2-2, albo 9 cyfr ciągiem po +48 lub "tel."; bez kwot (np. "100 000 000 zł").
    [GeneratedRegex(@"(?:(?<=\b(?i:tel\.?|telefon\w*)[ \t]*:?[ \t]*)(?<v>(?:(?:\+48|0048)[ ]?)?\d{9})\b)|(?<![\d+])(?<v>(?:(?:\+48|0048)[ ]?)?(?:\d{3}[ -]\d{3}[ -]\d{3}|\(?\d{2}\)?[ -]\d{3}[ -]\d{2}[ -]\d{2}))(?!\d)(?![ \t]*(?:zł|PLN|złot|EUR|USD|%))|(?<![\d+])(?<v>(?:\+48|0048)[ ]?\d{9})\b")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\p{L}+(?:-\p{L}+)*")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
