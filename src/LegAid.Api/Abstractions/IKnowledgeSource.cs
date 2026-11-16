using LegAid.Api.Model;

namespace LegAid.Api.Abstractions;

// Źródło przepisów dla agenta. Teraz to plik tekstowy (Infrastructure/Knowledge),
// później może to być baza wektorowa z wyszukiwaniem, bez zmian w logice agenta.
public interface IKnowledgeSource
{
    IReadOnlyList<KnowledgeFragment> GetAll();
}
