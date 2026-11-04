namespace LegAid.Api.Configuration;

public sealed class KnowledgeOptions
{
    public const string SectionName = "Knowledge";

    // false = agent odpowiada bez bazy przepisów, tylko z wiedzy modelu.
    public bool Enabled { get; set; } = true;
}
