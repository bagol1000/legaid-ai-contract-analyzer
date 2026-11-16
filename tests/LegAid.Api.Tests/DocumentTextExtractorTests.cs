using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LegAid.Api.Infrastructure.Documents;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

public class DocumentTextExtractorTests
{
    private readonly DocumentTextExtractor _extractor = new();

    [Fact]
    public async Task Docx_PutsEachParagraphOnItsOwnLine()
    {
        var docx = CreateDocx("UMOWA NAJMU", "", "§ 1. Przedmiotem najmu jest lokal przy ul. Źródlanej.", "§ 2. Czynsz wynosi 2800 zł.");

        var text = await _extractor.ExtractTextAsync(new MemoryStream(docx), ".docx");

        Assert.Equal("UMOWA NAJMU\n§ 1. Przedmiotem najmu jest lokal przy ul. Źródlanej.\n§ 2. Czynsz wynosi 2800 zł.", text);
    }

    [Fact]
    public async Task Docx_ReadsParagraphsInTables()
    {
        var docx = CreateDocx(body => body.Append(new Table(new TableRow(
            new TableCell(new Paragraph(new Run(new Text("Wynajmujący")))),
            new TableCell(new Paragraph(new Run(new Text("Najemca"))))))));

        var text = await _extractor.ExtractTextAsync(new MemoryStream(docx), ".DOCX");

        Assert.Equal("Wynajmujący\nNajemca", text);
    }

    [Fact]
    public async Task Pdf_KeepsLinesAndPageOrder()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var first = builder.AddPage(595, 842);
        first.AddText("UMOWA NAJMU", 12, new PdfPoint(50, 780), font);
        first.AddText("Par. 1. Przedmiot najmu", 12, new PdfPoint(50, 750), font);
        builder.AddPage(595, 842).AddText("Par. 2. Czynsz", 12, new PdfPoint(50, 780), font);

        var text = await _extractor.ExtractTextAsync(new MemoryStream(builder.Build()), ".pdf");

        Assert.Equal("UMOWA NAJMU\nPar. 1. Przedmiot najmu\nPar. 2. Czynsz", text);
    }

    [Fact]
    public async Task UnsupportedExtension_Throws() =>
        await Assert.ThrowsAsync<NotSupportedException>(() => _extractor.ExtractTextAsync(new MemoryStream([1, 2, 3]), ".txt"));

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    public async Task CorruptedFile_ThrowsInvalidData(string extension) =>
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            _extractor.ExtractTextAsync(new MemoryStream("to nie jest dokument"u8.ToArray()), extension));

    private static byte[] CreateDocx(params string[] paragraphs) =>
        CreateDocx(body => body.Append(paragraphs.Select(p => new Paragraph(new Run(new Text(p))))));

    private static byte[] CreateDocx(Action<Body> fill)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var body = new Body();
            fill(body);
            document.AddMainDocumentPart().Document = new Document(body);
        }

        return stream.ToArray();
    }
}
