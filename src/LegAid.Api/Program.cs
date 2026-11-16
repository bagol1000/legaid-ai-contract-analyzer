using LegAid.Api.Abstractions;
using LegAid.Api.Infrastructure;
using LegAid.Api.Infrastructure.Anonymization;
using LegAid.Api.Infrastructure.Documents;
using LegAid.Api.Infrastructure.Knowledge;
using LegAid.Api.Logic;
using LegAid.Api.Logic.ContractAnalysis;
using LegAid.Api.Presentation.Endpoints;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

// Composition root: jedyne miejsce, które zna wszystkie warstwy i skleja je przez DI.
// Warstwy: Model, Abstractions, Logic, Infrastructure, Presentation (opis w docs/ARCHITEKTURA.md).
var builder = WebApplication.CreateBuilder(args);

// User-secrets domyślnie ładują się tylko w Development. Gdy aplikacja startuje bez profilu
// z launchSettings.json (np. uruchomienie DLL/exe), środowisko to Production i klucz byłby pusty.
// Ładujemy je więc zawsze, a zmienne środowiskowe dodajemy ponownie, by zachowały pierwszeństwo.
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
    builder.Configuration.AddEnvironmentVariables();
}

// Model jest w appsettings.json, klucz w user-secrets albo w zmiennej OpenAI__ApiKey.
builder.Services.AddOptions<OpenAIOptions>()
    .Bind(builder.Configuration.GetSection(OpenAIOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey),
        "Brak klucza OpenAI:ApiKey. Ustaw: dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\" albo zmienną OpenAI__ApiKey.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Model),
        "Brak OpenAI:Model w appsettings.json.")
    .ValidateOnStart();

builder.Services
    .AddChatClient(sp =>
    {
        var openAI = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;

        return new OpenAIClient(openAI.ApiKey)
            .GetChatClient(openAI.Model)
            .AsIChatClient();
    })
    .UseFunctionInvocation()
    .UseLogging();

builder.Services.Configure<KnowledgeOptions>(builder.Configuration.GetSection(KnowledgeOptions.SectionName));
builder.Services.AddSingleton<IKnowledgeSource>(
    new FileKnowledgeSource(Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Knowledge", "przepisy.txt")));

builder.Services.AddSingleton<IAnonymizer, RegexAnonymizer>();
builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddSingleton<AgentMessages>();

builder.Services.AddScoped<ILegalAgent, LegalAgent>();
builder.Services.AddScoped<ContractAnalystAgent>();
builder.Services.AddScoped<ClauseReviewerAgent>();
builder.Services.AddScoped<ReportVerifierAgent>();
builder.Services.AddScoped<ContractAnalyzer>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.Logger.LogInformation("Baza wiedzy: wczytano {Count} przepisów", app.Services.GetRequiredService<IKnowledgeSource>().GetAll().Count);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
}

// Prezentacja: czat z wwwroot/index.html pod adresem /. Folder zostaje w korzeniu projektu,
// bo ASP.NET Core domyślnie serwuje i publikuje pliki statyczne właśnie z wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapAgentEndpoints();
app.MapContractEndpoints();

app.Run();
