using LegAid.Api.Model;

namespace LegAid.Api.Abstractions;

// Usuwa dane osobowe, zanim tekst trafi do modelu. Wszystkie teksty jednego wywołania mają wspólną numerację:
// ta sama wartość dostaje ten sam znacznik, np. [OSOBA_1], a numeracja zaczyna się za znacznikami już obecnymi w tekstach.
public interface IAnonymizer
{
    AnonymizationResult Anonymize(IReadOnlyList<string> texts);
}
