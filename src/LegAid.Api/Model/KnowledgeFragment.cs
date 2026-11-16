namespace LegAid.Api.Model;

/// <summary>Jeden artykuł z bazy przepisów.</summary>
/// <param name="Source">Etykieta, np. "KC art. 483".</param>
/// <param name="Text">Treść artykułu.</param>
public sealed record KnowledgeFragment(string Source, string Text);
