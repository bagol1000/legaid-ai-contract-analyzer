namespace LegAid.Api.Model;

/// <summary>Rodzaj danych osobowych usuwanych przed wysłaniem tekstu do modelu.</summary>
public enum PersonalDataKind
{
    Person,
    Pesel,
    Nip,
    Regon,
    IdCard,
    BankAccount,
    Email,
    Phone,
    Address,
    BirthDate
}

/// <param name="Texts">Teksty po anonimizacji, w tej samej kolejności co na wejściu.</param>
/// <param name="Removed">Ile różnych wartości danego rodzaju zastąpiono znacznikami.</param>
public sealed record AnonymizationResult(
    IReadOnlyList<string> Texts,
    IReadOnlyDictionary<PersonalDataKind, int> Removed);
