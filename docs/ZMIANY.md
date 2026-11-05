# Zmiany w wersji wczesnego demo

Co było na początku, co zmieniliśmy i co warto jeszcze zrobić na obecnym etapie. Punkt wyjścia to commit `f74e2b1`, zmiany są w `06c2984`.

## Stan początkowy

- API w ASP.NET Core (.NET 10) z jednym endpointem `POST /api/agent/ask`, bez interfejsu. Pytania wysyłało się komendami z PowerShella.
- Model `gpt-5-nano`.
- Każde pytanie było osobne, bez pamięci rozmowy.
- Baza wiedzy to 19 artykułów w `Knowledge/przepisy.txt`, wyszukiwanie działa lokalnie.
- Na komputerach z włączonym Smart App Control `dotnet run` kończył się błędem, bo Windows blokował niepodpisany `LegAid.Api.exe`.

## Co zostało zmienione

### Czat w przeglądarce

Nowy plik `wwwroot/index.html` zawiera cały interfejs: HTML, CSS i JavaScript, bez npm i budowania frontendu. `Program.cs` serwuje go pod `http://localhost:5080`.

Czat pokazuje czas oczekiwania, formatuje odpowiedzi i wyróżnia powołania na przepisy. Gdy zapytanie się nie uda, tekst pytania wraca do pola wpisywania.

### Pamięć rozmowy

- `POST /api/agent/ask` przyjmuje opcjonalne pole `history`. Samo `message` działa jak wcześniej.
- Historię trzyma przeglądarka i wysyła ją z każdym pytaniem, a serwer niczego nie zapisuje. Odświeżenie strony czyści rozmowę.
- Model dostaje 20 ostatnich wiadomości.
- Dozwolone role to tylko `user` i `assistant`, inne dają błąd 400.
- Przepisy są szukane po bieżącym i poprzednim pytaniu, żeby działały nawiązania typu „a co z § 7?”.

### Model

`gpt-5-nano` zastąpiony przez `gpt-6-luna` w `appsettings.json`. Porównanie na 5 pytaniach z demo, po jednym przebiegu, z włączoną bazą:

| | `gpt-5-nano` | `gpt-6-luna` |
|---|---|---|
| Czas odpowiedzi | 24–48 s | 7–18 s |
| Tokeny wyjścia | 4490–8628 | 486–1568 |
| Art. 483 KC przy karze za czynsz | Pominięty | Użyty poprawnie |
| Art. 385³ pkt 23 przy sądzie klubu | Pominięty | Użyty poprawnie |

### Uruchamianie

W `LegAid.Api.csproj` dodane jest `<UseAppHost>false</UseAppHost>`. Projekt nie tworzy już pliku `.exe`, więc `dotnet run` działa także przy włączonym Smart App Control.

### Cała baza zamiast wyszukiwania

Przy 19 artykułach wyszukiwanie częściej szkodziło, niż pomagało, np. gubiło art. 117 KC przy wezwaniu do zapłaty. Teraz `LegalAgent` wysyła całą bazę w każdym zapytaniu, zaraz po prompcie systemowym, a `KnowledgeBase` tylko wczytuje przepisy. Wyszukiwanie wróci jako RAG z embeddingami, gdy baza urośnie.

### Odpowiedzi tylko z bazy

Przy włączonej bazie model opiera się wyłącznie na niej, a gdy czegoś w niej nie ma, pisze „Nie mam informacji na ten temat.”. O samej bazie w odpowiedziach nie wspomina. Reguły są w `KnowledgeRules` w `LegalAgentPrompts.cs`. Przy `Knowledge:Enabled = false` nie obowiązują i agent odpowiada z wiedzy modelu.

## Do zrobienia na teraz

### Dokumentacja

- [ ] `DEMO.md`: dopisać uruchamianie czatu w przeglądarce.
- [ ] `DEMO.md`: usunąć zapowiedź, że etykieta zawsze oznacza przepis z bazy (szczegóły niżej).
- [x] `PREZENTACJA.md`: zmienić model, dodać historię do schematu i usunąć frontend z „Co dalej”.

### Baza wiedzy

- [ ] Sprawdzić w ISAP art. 117 § 3 KC, oznaczony w `przepisy.txt` jako uchylony. Nie udało się tego potwierdzić.
- [x] Sprawdzić na pytaniach z demo, czy model używa art. 117 § 2¹ KC przy wezwaniu do zapłaty. Używa.
- [x] Dodać art. 481 KC (odsetki), brzmienie z Dz.U. 2026 poz. 795.
- [x] Art. 18 KP przy pytaniu o urlop: pytanie nie dotyczy zapisu umowy, więc poprawiona została zapowiedź w `DEMO.md`.

### Przed pokazem

- [ ] Przejść wszystkie pytania z demo w przeglądarce i zapisać dobre odpowiedzi na wypadek braku sieci.
- [ ] Sprawdzić koszt `gpt-6-luna` na koncie OpenAI.
- [ ] Sprawdzić wygląd czatu w trybie ciemnym, bo nie był oglądany.

## Znane ograniczenia

Na tym etapie można je zostawić, ale warto o nich wiedzieć przy pokazie:

- Ograniczenie do bazy wynika tylko z promptu, kod go nie wymusza. Etykiety warto sprawdzać w `Knowledge/przepisy.txt`.
- Baza ma 20 artykułów, więc na wiele zwykłych pytań agent odpowie, że nie ma informacji.
- Każde zapytanie niesie całą bazę, więc koszt wejścia rośnie razem z nią. To działa tylko przy małej bazie.
- Model dostaje też przepisy niezwiązane z pytaniem i ma je pomijać zgodnie z promptem.
- Jakość odpowiedzi nie była oceniona przez prawnika.

## Poza zakresem wczesnego demo

Plany z `Wizja.md`, na razie niezrobione:

- wgrywanie PDF i DOCX,
- raport ryzyk w JSON,
- kilku współpracujących agentów i narzędzia dla modelu,
- generowanie poprawionej wersji umowy,
- pełne teksty ustaw i wyszukiwanie przez embeddingi,
- testy automatyczne.
