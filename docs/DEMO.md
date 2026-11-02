# Demo — ściąga

## Start

```powershell
$env:OpenAI__ApiKey = "sk-..."    # albo raz: dotnet user-secrets set "OpenAI:ApiKey" "sk-..." (w src\LegAid.Api)
cd src\LegAid.Api
dotnet run                        # http://localhost:5080
```

Bez klucza aplikacja nie wstanie i od razu powie, czego brakuje.

## Zapytania

W drugim oknie (PowerShell 7) wklejamy raz:

```powershell
function ask($text) {
    $body = @{ message = $text } | ConvertTo-Json
    $res = Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post `
        -ContentType "application/json; charset=utf-8" `
        -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 120 -SkipHttpErrorCheck
    $res.answer ?? ($res | ConvertTo-Json -Depth 5)
}
```

Potem już tylko:

```powershell
ask "Czy pracodawca może wyłączyć prawo do urlopu w pierwszym roku pracy?"
ask "Przeanalizuj umowę najmu, najemca jest konsumentem. § 5. Kara umowna 1000 zł za każdy dzień opóźnienia w czynszu. § 6. Wynajmujący może wypowiedzieć umowę w każdym czasie. § 7. Kaucja nie podlega zwrotowi."
ask "Dostałem takie pismo, co mam zrobić? 'Wzywamy do zapłaty 2 340 zł z faktury nr 14/2021 w terminie 7 dni, inaczej sprawa trafi do sądu.'"
ask ""    # 400, nie idzie do modelu
```

Te same zapytania są w `src/LegAid.Api/LegAid.Api.http` (Visual Studio / Rider / VS Code z REST Client).
Odpowiedź trwa kilkanaście sekund, warto mieć jedną przygotowaną wcześniej na wypadek problemów z siecią.

## Co pokazać w kodzie

- `Program.cs` — `IChatClient` z OpenAI w DI, klucz poza repo, walidacja konfiguracji przy starcie
- `Agents/LegalAgentPrompts.cs` — cała „wiedza” agenta to na razie ten prompt
- `Agents/LegalAgent.cs` — wywołanie modelu i log zużycia tokenów (widać w konsoli serwera)
- `Endpoints/AgentEndpoints.cs` — endpoint i walidacja
