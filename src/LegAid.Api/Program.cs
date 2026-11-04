using LegAid.Api.Agents;
using LegAid.Api.Configuration;
using LegAid.Api.Endpoints;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

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
builder.Services.AddSingleton(new KnowledgeBase(Path.Combine(AppContext.BaseDirectory, "Knowledge", "przepisy.txt")));

builder.Services.AddScoped<ILegalAgent, LegalAgent>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.Logger.LogInformation("Baza wiedzy: wczytano {Count} przepisów", app.Services.GetRequiredService<KnowledgeBase>().Count);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
}

app.MapAgentEndpoints();

app.Run();
