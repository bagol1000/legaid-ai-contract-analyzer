# LegAid

Asystent AI do analizy umów i pism prawnych według prawa polskiego.
Tworzenie agentów AI w .NET, PW. Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski.

Notatki do pokazu. Komendy i kolejność kroków są w `DEMO.md`.

## Problem

Większość ludzi podpisuje umowę najmu albo regulamin siłowni bez czytania drobnego druku, a o prawnika nikt nie pyta. To samo z wezwaniem do zapłaty: list przychodzi, jest strach, a nie wiadomo, czy dług w ogóle jest jeszcze wymagalny.

LegAid dostaje pytanie albo tekst umowy czy pisma i odpowiada zwykłym językiem, z podaniem przepisu. Przy umowie mówi, które zapisy są problemem i jak je poprawić.

## Jak to działa

```
POST /api/agent/ask  { "message": "..." }
   -> AgentEndpoints   sprawdza, czy wiadomość nie jest pusta
   -> LegalAgent       szuka pasujących przepisów w bazie
   -> KnowledgeBase    3-4 przepisy z Knowledge/przepisy.txt
   -> IChatClient      prompt + przepisy + pytanie
   -> OpenAI           gpt-5-nano
<- { "answer": "...", "model": "..." }
```

ASP.NET Core (.NET 10), Microsoft.Extensions.AI, OpenAI. Agent nie wie, z jakim modelem rozmawia, zna tylko `IChatClient`. Model zmienia się w `appsettings.json`.

## Baza wiedzy

Bez bazy model odpowiada z pamięci i potrafi pomylić numer artykułu. Dlatego dołożyliśmy mały RAG: plik tekstowy z 19 przepisami z KC, KP, ustawy o ochronie praw lokatorów i ustawy o BIG, dobranymi pod nasze przykłady.

Przed każdym pytaniem agent szuka w nim pasujących przepisów (BM25, lokalnie) i dokleja je do rozmowy. Model oznacza je w odpowiedzi, np. `[KC art. 484]`, a w konsoli serwera widać, co zostało znalezione.

## Prompt

W `Agents/LegalAgentPrompts.cs`. Najważniejsze zasady: przepisy z bazy streszcza własnymi słowami i podaje ich etykietę, innych przepisów nie zmyśla, a na końcu zaznacza, że to nie jest porada prawna.

## Pokaz

1. urlop w pierwszym roku pracy
2. umowa najmu z trzema złymi zapisami
3. kaucja nieoddana po wyprowadzce
4. regulamin siłowni
5. wezwanie do zapłaty ze starej faktury
6. pusta wiadomość, czyli błąd 400

## Co dalej

- odpowiedź w JSON (lista ryzyk zamiast tekstu)
- wgrywanie PDF i DOCX
- pełne teksty KC i KP w bazie i wyszukiwanie przez embeddingi
- testy jakości odpowiedzi na zestawie przykładowych umów
- frontend
