using LegAid.Api.Agents;
using LegAid.Api.Configuration;
using LegAid.Api.Endpoints;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
    builder.Configuration.AddEnvironmentVariables();
}

builder.Services.AddOptions<OpenAIOptions>()
    .Bind(builder.Configuration.GetSection(OpenAIOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey),
        "Brak klucza OpenAI:ApiKey. Ustaw: dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\" albo zmienną OpenAI__ApiKey.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Model),
        "Brak OpenAI:Model w konfiguracji.")
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

builder.Services.AddScoped<ILegalAgent, LegalAgent>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapAgentEndpoints();

app.Run();
