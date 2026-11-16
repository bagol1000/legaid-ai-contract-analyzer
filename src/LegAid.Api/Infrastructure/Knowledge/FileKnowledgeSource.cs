using LegAid.Api.Abstractions;
using LegAid.Api.Model;

namespace LegAid.Api.Infrastructure.Knowledge;

// Przepisy z pliku przepisy.txt, wczytane raz przy starcie, jeden artykuł = jeden fragment.
// Baza jest mała, więc agent dostaje ją w całości, bez wyszukiwania.
public sealed class FileKnowledgeSource(string path) : IKnowledgeSource
{
    private readonly IReadOnlyList<KnowledgeFragment> _fragments = KnowledgeFileParser.Parse(File.ReadLines(path));

    public IReadOnlyList<KnowledgeFragment> GetAll() => _fragments;
}
