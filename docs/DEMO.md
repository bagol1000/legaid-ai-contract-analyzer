# Demo — LegAid (zajęcia, czwartek)

Agent prawny: ASP.NET Core Web API (.NET 10) → `IChatClient` (Microsoft.Extensions.AI) → OpenAI `gpt-5-nano`.

## 1. Uruchomienie

```powershell
$env:OpenAI__ApiKey = "sk-..."          # albo: dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
cd src\LegAid.Api
dotnet run                              # Now listening on: http://localhost:5080
```

## 2. Zapytania (nowe okno PowerShell)

Pytanie prawne:

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Czy pracodawca może wyłączyć prawo do urlopu w pierwszym roku pracy?"}')) -TimeoutSec 120).answer
```

Analiza fragmentu umowy:

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Przeanalizuj umowę najmu (najemca jest konsumentem): § 5. Kara umowna 1000 zł za każdy dzień opóźnienia w czynszu. § 6. Wynajmujący może wypowiedzieć umowę w każdym czasie bez okresu wypowiedzenia. § 7. Kaucja nie podlega zwrotowi."}')) -TimeoutSec 120).answer
```

Walidacja (pusty tekst → `400 Bad Request`):

```powershell
Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json" -Body '{"message":""}'
```

To samo jest w `src/LegAid.Api/LegAid.Api.http` (VS / Rider: **Send request**), opis API: `http://localhost:5080/openapi/v1.json`.

## 3. Co pokazać

| Punkt | Gdzie w kodzie |
|---|---|
| `IChatClient` z OpenAI rejestrowany w DI (`AddChatClient(...).UseFunctionInvocation()`) | `Program.cs` |
| Klucz poza repo (zmienna `OpenAI__ApiKey` / user-secrets), walidacja przy starcie | `Configuration/OpenAIOptions.cs`, `Program.cs` |
| System prompt: asystent prawny, polskie prawo, odpowiedzi po polsku | `Agents/LegalAgentPrompts.cs` |
| Serwis agenta — miejsce na narzędzia (`search_law`) i kontekst z RAG | `Agents/LegalAgent.cs` |
| Endpoint `POST /api/agent/ask` → `{ answer, model }` | `Endpoints/AgentEndpoints.cs` |

## 4. Dalej

Structured output (lista ryzyk w JSON) → upload PDF/DOCX → RAG z KC/KP → narzędzie `search_law` → ewaluacja → frontend.
