namespace LegAid.Api.Abstractions;

// Wyciąga tekst z wgranego dokumentu. Każdy akapit jest w osobnym wierszu, więc nagłówki "§ 5" zaczynają wiersz.
public interface IDocumentTextExtractor
{
    /// <summary>Obsługiwane rozszerzenia, małymi literami, z kropką, np. ".pdf".</summary>
    IReadOnlyCollection<string> SupportedExtensions { get; }

    Task<string> ExtractTextAsync(Stream content, string extension, CancellationToken cancellationToken = default);
}
