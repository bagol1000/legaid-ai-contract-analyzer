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

1. Pytanie: czy pracodawca może wyłączyć urlop w pierwszym roku pracy?
2. Umowa najmu z karą 1000 zł dziennie, wypowiedzeniem „w każdym czasie” i bezzwrotną kaucją
3. Wezwanie do zapłaty z faktury z 2021 r.
4. Pusta wiadomość: 400, model nie jest wywoływany

Komendy są w `docs/DEMO.md`.

---

## Co dalej

Dziś model odpowiada z własnej wiedzy i nic tego nie sprawdza. Plan:

1. odpowiedź jako lista ryzyk w JSON zamiast samego tekstu
2. wgrywanie PDF i DOCX
3. RAG na tekstach KC i KP + narzędzie `search_law`, żeby przepisy dało się zweryfikować
4. ocena jakości odpowiedzi na zestawie przykładowych dokumentów
5. frontend
