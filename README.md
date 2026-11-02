# LegAiD — agent AI do analizy umów (polskie prawo)

**Autorzy:** Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

Projekt na przedmiot „Tworzenie agentów AI w .NET” (PW). Wizja produktu: [`docs/Wizja.md`](docs/Wizja.md).
ASP.NET Core Web API (.NET 10) + Microsoft.Extensions.AI (`IChatClient`) + OpenAI.

## Uruchomienie

```powershell
cd src/LegAid.Api
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."   # klucz zostaje poza repo
dotnet run                                          # http://localhost:5080
```

Zamiast user-secrets można ustawić zmienną środowiskową `OpenAI__ApiKey`.
Model zmienia się w `appsettings.json` (`OpenAI:Model`, domyślnie `gpt-5-nano`).

## Demo

Scenariusz pokazu na zajęciach: [`docs/DEMO.md`](docs/DEMO.md).

## Test

- `src/LegAid.Api/LegAid.Api.http` — gotowe zapytania (VS / Rider / VS Code REST Client),
- OpenAPI: `http://localhost:5080/openapi/v1.json`.

```
POST /api/agent/ask
{ "message": "pytanie albo treść umowy" }
→ { "answer": "...", "model": "gpt-5-nano-..." }
```

## Struktura

```
src/LegAid.Api/
  Program.cs                 DI: opcje OpenAI, IChatClient, agent, OpenAPI
  Configuration/             OpenAIOptions
  Agents/                    ILegalAgent, LegalAgent, system prompt
  Endpoints/                 POST /api/agent/ask
```
