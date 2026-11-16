namespace LegAid.Api.Infrastructure.Anonymization;

// Popularne polskie imiona i rozpoznawanie ich odmienionych form, np. "Tomaszem", "Katarzyną", "Pawła".
internal static class PolishFirstNames
{
    private static readonly string[] Names =
    [
        // męskie
        "Adam", "Adrian", "Aleksander", "Andrzej", "Antoni", "Arkadiusz", "Artur", "Bartosz", "Bogdan", "Bogusław",
        "Borys", "Cezary", "Czesław", "Damian", "Daniel", "Dariusz", "Dawid", "Dominik", "Edward", "Emil",
        "Eugeniusz", "Filip", "Franciszek", "Grzegorz", "Henryk", "Hubert", "Ignacy", "Igor", "Ireneusz", "Jacek",
        "Jakub", "Jan", "Janusz", "Jarosław", "Jerzy", "Józef", "Julian", "Kacper", "Kamil", "Karol",
        "Kazimierz", "Konrad", "Krystian", "Krzysztof", "Leon", "Leszek", "Ludwik", "Łukasz", "Maciej", "Marcel",
        "Marcin", "Marek", "Marian", "Mariusz", "Mateusz", "Michał", "Mieczysław", "Mikołaj", "Mirosław", "Nikodem",
        "Norbert", "Oliwer", "Oskar", "Patryk", "Paweł", "Piotr", "Przemysław", "Radosław", "Rafał", "Robert",
        "Roman", "Ryszard", "Sebastian", "Sławomir", "Stanisław", "Stefan", "Szymon", "Tadeusz", "Tomasz", "Tymon",
        "Waldemar", "Wiesław", "Wiktor", "Witold", "Władysław", "Wojciech", "Zbigniew", "Zdzisław", "Zenon",
        // żeńskie
        "Adrianna", "Agata", "Agnieszka", "Aleksandra", "Alicja", "Amelia", "Aniela", "Angelika", "Anita", "Anna",
        "Antonina", "Barbara", "Beata", "Bogumiła", "Bożena", "Czesława", "Danuta", "Daria", "Diana", "Dominika",
        "Dorota", "Edyta", "Elżbieta", "Elwira", "Emilia", "Ewa", "Ewelina", "Gabriela", "Genowefa", "Grażyna",
        "Halina", "Hanna", "Helena", "Ilona", "Irena", "Iwona", "Izabela", "Jadwiga", "Jagoda", "Janina",
        "Joanna", "Jolanta", "Julia", "Justyna", "Kamila", "Karolina", "Katarzyna", "Kazimiera", "Kinga", "Klaudia",
        "Krystyna", "Laura", "Lena", "Leokadia", "Lidia", "Liliana", "Lucyna", "Magdalena", "Maja", "Małgorzata",
        "Maria", "Marianna", "Marta", "Martyna", "Marzena", "Michalina", "Milena", "Mirosława", "Monika", "Natalia",
        "Nikola", "Nina", "Oliwia", "Patrycja", "Paulina", "Regina", "Renata", "Roksana", "Sandra", "Stanisława",
        "Stefania", "Sylwia", "Teresa", "Urszula", "Wanda", "Weronika", "Wiesława", "Wiktoria", "Wioletta", "Zofia",
        "Zuzanna"
    ];

    // Końcówki odmiany doklejane do rdzenia; najdłuższe pierwsze.
    private static readonly string[] Endings = ["owi", "em", "ie", "ze", "ej", "a", "y", "ą", "ę", "u", "i", "o", ""];

    private static readonly HashSet<string> Stems = Names.SelectMany(StemsOf).ToHashSet();

    public static bool IsFirstName(string word) =>
        Endings.Any(e => word.EndsWith(e, StringComparison.Ordinal) && Stems.Contains(word[..^e.Length]));

    // Rdzeń bez końcowego -a; dla -eł i -ek także postać bez "e", np. Paweł -> Pawł(a), Marek -> Mark(a).
    private static IEnumerable<string> StemsOf(string name)
    {
        if (name.EndsWith('a'))
        {
            yield return name[..^1];
            yield break;
        }

        yield return name;
        if (name.EndsWith("eł") || name.EndsWith("ek"))
        {
            yield return name[..^2] + name[^1];
        }
    }
}
