# LegAid

**Agent AI do analizy umów według polskiego prawa**

Projekt na przedmiot „Tworzenie agentów AI w .NET” (PW)

---

## Problem

Umowę najmu, zlecenia czy pracę podpisuje się często bez konsultacji z prawnikiem,
więc niekorzystne zapisy wychodzą dopiero przy sporze.

**LegAid** przyjmuje pytanie prawne albo treść umowy i:

- wskazuje postanowienia niezgodne z prawem,
- wskazuje klauzule ryzykowne i potencjalnie abuzywne (art. 385¹ i nast. KC),
- wskazuje luki, czyli brakujące istotne postanowienia,
- podaje podstawę prawną i proponuje poprawione brzmienie.

---

## Architektura

```
klient HTTP
    │  POST /api/agent/ask
    ▼
AgentEndpoints        walidacja, kontrakt { answer, model }
    ▼
ILegalAgent           system prompt + wiadomość użytkownika
    ▼
IChatClient           Microsoft.Extensions.AI: wywoływanie narzędzi, logowanie
    ▼
OpenAI                gpt-5-nano
```

**Stack:** ASP.NET Core Web API (.NET 10), Microsoft.Extensions.AI, OpenAI, OpenAPI

Agent zależy tylko od `IChatClient`, więc model można zmienić w `appsettings.json`,
a narzędzia dodać bez zmian w rejestracji klienta.

---

## System prompt

Zachowanie agenta definiuje `Agents/LegalAgentPrompts.cs`:

- specjalizacja: prawo polskie (KC, KP, ustawa o prawach konsumenta),
- przepisy powołuje tylko wtedy, gdy ma pewność,
- nie wymyśla przepisów ani orzeczeń,
- mówi wprost, gdy czegoś nie wie lub stan prawny może być nieaktualny,
- zaznacza, że odpowiedź nie stanowi porady prawnej.

---

## Uruchomienie

```powershell
cd src\LegAid.Api
$env:OpenAI__ApiKey = "sk-..."
dotnet run    # http://localhost:5080
```

Kontrakt API:

```
POST /api/agent/ask
{ "message": "pytanie albo treść umowy" }
→ 200 { "answer": "...", "model": "gpt-5-nano-..." }
→ 400 gdy "message" jest puste
```

---

## Przykład 1: pytanie prawne

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Czy pracodawca może wyłączyć prawo do urlopu w pierwszym roku pracy?"}')) -TimeoutSec 120).answer
```

---

## Przykład 2: analiza umowy najmu

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Przeanalizuj umowę najmu (najemca jest konsumentem): § 5. Kara umowna 1000 zł za każdy dzień opóźnienia w czynszu. § 6. Wynajmujący może wypowiedzieć umowę w każdym czasie bez okresu wypowiedzenia. § 7. Kaucja nie podlega zwrotowi."}')) -TimeoutSec 120).answer
```

---

## Przykład 3: analiza umowy zlecenia

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Przeanalizuj umowę zlecenia: § 3. Zleceniobiorca przez 5 lat po zakończeniu umowy nie może pracować dla konkurencji i nie otrzymuje z tego tytułu odszkodowania. § 4. Zleceniodawca może jednostronnie obniżyć wynagrodzenie. § 5. Wszelkie spory rozstrzyga sąd właściwy dla siedziby Zleceniodawcy."}')) -TimeoutSec 120).answer
```

---

## Przykład 4: walidacja

Pusta wiadomość nie trafia do modelu. API zwraca `400` i opis błędu:

```powershell
try { Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json" -Body '{"message":""}' } catch { $_.Exception.Response.StatusCode.value__; $s = $_.Exception.Response.GetResponseStream(); $s.Position = 0; (New-Object IO.StreamReader($s)).ReadToEnd() }
```

---

## Przykład 5: pełna odpowiedź i opis API

Odpowiedź z nazwą modelu, który ją wygenerował:

```powershell
Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Jaki jest okres wypowiedzenia umowy o pracę na czas nieokreślony po 4 latach zatrudnienia?"}')) -TimeoutSec 120 | Format-List
```

Dokument OpenAPI (środowisko Development):

```powershell
Invoke-RestMethod http://localhost:5080/openapi/v1.json | ConvertTo-Json -Depth 10
```

---

## Stan projektu i dalsze kroki

**Działa dziś:** jeden endpoint, system prompt, walidacja wejścia, logowanie zużycia tokenów.

**Dalej:**

1. Structured output: lista ryzyk w JSON
2. Upload PDF/DOCX
3. RAG z KC i KP
4. Narzędzie `search_law`
5. Ewaluacja jakości odpowiedzi
6. Frontend

Odpowiedzi modelu nie są dziś weryfikowane względem tekstu przepisów. Ma to zmienić RAG i `search_law`.
