# LegAid

Asystent AI do analizy umów i pism prawnych według prawa polskiego

Tworzenie agentów AI w .NET, PW
Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

---

## Problem

Umowy najmu, zlecenia czy o pracę zwykle podpisuje się bez prawnika.
Podobnie z wezwaniem do zapłaty albo wypowiedzeniem: trzeba je zrozumieć, zanim minie termin.

LegAid przyjmuje pytanie albo tekst dokumentu. W umowie szuka zapisów niezgodnych z prawem,
klauzul abuzywnych i braków, a do każdego problemu podaje przepis i proponuje lepsze brzmienie.
Pismo tłumaczy na zwykły język: o co chodzi, do kiedy i co można zrobić.

---

## Architektura

```
POST /api/agent/ask  { "message": "..." }
        │
AgentEndpoints   walidacja
        │
LegalAgent       system prompt + wiadomość
        │
IChatClient      Microsoft.Extensions.AI
        │
OpenAI           gpt-5-nano
        │
{ "answer": "...", "model": "..." }
```

ASP.NET Core (.NET 10), Microsoft.Extensions.AI, OpenAI.
Agent zna tylko `IChatClient`, więc zmiana modelu to jedna linia w `appsettings.json`.

---

## Prompt

`Agents/LegalAgentPrompts.cs`:

- prawo polskie: KC, KP, prawa konsumenta
- osobny tryb dla umów i dla pozostałych pism
- przepis podaje tylko, gdy jest pewien; nie wymyśla orzeczeń
- na końcu zastrzeżenie, że to nie porada prawna

---

## Demo

```powershell
ask "Czy pracodawca może wyłączyć prawo do urlopu w pierwszym roku pracy?"

ask "Przeanalizuj umowę najmu, najemca jest konsumentem. § 5. Kara umowna 1000 zł
     za każdy dzień opóźnienia w czynszu. § 6. Wynajmujący może wypowiedzieć umowę
     w każdym czasie. § 7. Kaucja nie podlega zwrotowi."

ask "Dostałem takie pismo, co mam zrobić? 'Wzywamy do zapłaty 2 340 zł z faktury
     nr 14/2021 w terminie 7 dni, inaczej sprawa trafi do sądu.'"

ask ""   # 400, pusty tekst nie trafia do modelu
```

(`ask` to kilkulinijkowa funkcja z `docs/DEMO.md`)

---

## Co dalej

Dziś model odpowiada z własnej wiedzy i nic tego nie sprawdza. Plan:

1. odpowiedź jako lista ryzyk w JSON zamiast samego tekstu
2. wgrywanie PDF i DOCX
3. RAG na tekstach KC i KP + narzędzie `search_law`, żeby przepisy dało się zweryfikować
4. ocena jakości odpowiedzi na zestawie przykładowych dokumentów
5. frontend
