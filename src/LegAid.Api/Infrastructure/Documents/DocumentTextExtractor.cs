using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LegAid.Api.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LegAid.Api.Infrastructure.Documents;

// Tekst z PDF (PdfPig) i DOCX (OpenXml), każdy akapit w osobnym wierszu, bez pustych wierszy.
public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = [".pdf", ".docx"];

    public async Task<string> ExtractTextAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var read = extension.ToLowerInvariant() switch
        {
            ".pdf" => (Func<Stream, IEnumerable<string>>)ReadPdf,
            ".docx" => ReadDocx,
            _ => throw new NotSupportedException($"Nieobsługiwany format pliku: {extension}.")
        };

        // Obie biblioteki potrzebują strumienia z dostępem swobodnym, a IFormFile go nie daje.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        try
        {
            return Normalize(read(buffer));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidDataException("Nie udało się odczytać pliku.", ex);
        }
    }

    private static readonly ContentOrderTextExtractor.Options PdfOptions = new()
    {
        SeparateParagraphsWithDoubleNewline = true,
        ReplaceWhitespaceWithSpace = true
    };

    // PDF zawija akapity na wiersze strony. Sklejamy je z powrotem, żeby np. numer telefonu
    // rozbity na dwa wiersze dalo się rozpoznać przy anonimizacji.
    private static IEnumerable<string> ReadPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        return document.GetPages()
            .SelectMany(page => ContentOrderTextExtractor.GetText(page, PdfOptions).ReplaceLineEndings("\n").Split("\n\n"))
            .Select(paragraph => string.Join(" ", paragraph.Split('\n', StringSplitOptions.TrimEntries)))
            .ToList();
    }

    private static IEnumerable<string> ReadDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body
            ?? throw new InvalidDataException("Dokument nie ma treści.");

        // Descendants obejmuje też akapity w tabelach, w kolejności z dokumentu.
        return body.Descendants<Paragraph>().Select(ParagraphText).ToList();
    }

    private static string ParagraphText(Paragraph paragraph) =>
        string.Concat(paragraph.Descendants().Select(element => element switch
        {
            Text text => text.Text,
            TabChar => "\t",
            Break or CarriageReturn => "\n",
            _ => ""
        }));

    // Jeden wiersz = jeden akapit: przycięte, bez pustych, twarde spacje zamienione na zwykłe (np. w "§ 5").
    private static string Normalize(IEnumerable<string> parts) =>
        string.Join("\n", parts
            .SelectMany(part => part.Replace(' ', ' ').Split(['\r', '\n']))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));
}
