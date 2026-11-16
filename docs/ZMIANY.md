# Zmiany w wersji wczesnego demo

Co było na początku, co zmieniliśmy i co warto jeszcze zrobić na obecnym etapie. Punkt wyjścia to commit `f74e2b1`, zmiany są w `06c2984` i `057455c`.

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
- Nawiązania typu „a co z § 7?” działają, bo model widzi wcześniejsze wiadomości, a cała baza przepisów idzie w każdym zapytaniu.

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

### Podział na warstwy

Kod jest podzielony na foldery według warstw: `Model`, `Abstractions`, `Logic`, `Infrastructure` i `Presentation`. Opis jest w `ARCHITEKTURA.md`. Działanie aplikacji i format API się nie zmieniły. Oprócz przeniesienia plików:

- `LegalAgent` zależy od interfejsu `IKnowledgeSource`, a nie od konkretnej klasy. Dawny `KnowledgeBase` to teraz `FileKnowledgeSource`.
- Czytanie pliku (`FileKnowledgeSource`) jest oddzielone od parsowania formatu przepisów (`KnowledgeFileParser`).
- API ma własne DTO (`ChatTurnDto`), a agent dostaje `ChatTurn` z modelu domeny z rolą jako enum. Wcześniej ta sama klasa służyła do obu celów.
- `przepisy.txt` jest w `Infrastructure/Knowledge/`.

### Streamowanie odpowiedzi

Czat pokazuje odpowiedź na bieżąco, w miarę jak model ją generuje. Korzysta z nowego endpointu `POST /api/agent/ask/stream`, który przyjmuje to samo co `/ask` i wysyła tekst jako Server-Sent Events (ADR-0006). Endpoint `/ask` działa jak wcześniej.

### Analiza wgranej umowy

Spinacz w czacie wgrywa umowę w PDF albo DOCX (`POST /api/contract/analyze`). Tekst jest najpierw anonimizowany, potem analizuje go trzech agentów: analityk, recenzenci paragrafów i weryfikator (ADR-0008). Czat pokazuje postęp i raport: ryzyka z poziomem, podstawą prawną i proponowanym brzmieniem, brakujące postanowienia i listę ukrytych danych. Po raporcie można dopytywać w czacie. Przykładowa umowa z fikcyjnymi danymi jest w `samples/`.

### Anonimizacja

Przed wysłaniem do modelu usuwamy dane osobowe, w umowach i w wiadomościach z czatu: imiona i nazwiska, PESEL, NIP, REGON, dowód, numer konta, e-mail, telefon, adres i datę urodzenia (ADR-0007). Zastępują je znaczniki, np. [OSOBA_1].

### Testy i ewaluacja

`tests/LegAid.Api.Tests`: testy jednostkowe i całego API z udawanym modelem, bez sieci. `tests/LegAid.Eval`: pytania z demo i przykładowe umowy na prawdziwym modelu, sprawdza powołane przepisy i wycieki danych. Jak uruchomić, opisuje `DEMO.md`.

## Do zrobienia na teraz

### Dokumentacja

- [x] `DEMO.md`: dopisać uruchamianie czatu w przeglądarce.
- [x] `DEMO.md`: usunąć zapowiedź, że etykieta zawsze oznacza przepis z bazy (szczegóły niżej).
- [x] `PREZENTACJA.md`: zmienić model, dodać historię do schematu i usunąć frontend z „Co dalej”.

### Baza wiedzy

- [ ] Sprawdzić w ISAP art. 117 § 3 KC, oznaczony w `przepisy.txt` jako uchylony. Nie udało się tego potwierdzić.
- [x] Sprawdzić na pytaniach z demo, czy model używa art. 117 § 2¹ KC przy wezwaniu do zapłaty. Używa.
- [x] Dodać art. 481 KC (odsetki), brzmienie z Dz.U. 2026 poz. 795.
- [x] Art. 18 KP przy pytaniu o urlop: pytanie nie dotyczy zapisu umowy, więc poprawiona została zapowiedź w `DEMO.md`.

### Przed pokazem

- [ ] Przejść wszystkie pytania z demo w przeglądarce i zapisać dobre odpowiedzi na wypadek braku sieci.
- [ ] Sprawdzić koszt `gpt-6-luna` na koncie OpenAI.
- [x] Sprawdzić wygląd czatu w trybie ciemnym.

## Znane ograniczenia

Na tym etapie można je zostawić, ale warto o nich wiedzieć przy pokazie:

- Ograniczenie do bazy wynika tylko z promptu, kod go nie wymusza. Etykiety warto sprawdzać w `Infrastructure/Knowledge/przepisy.txt`.
- Baza ma 20 artykułów, więc na wiele zwykłych pytań agent odpowie, że nie ma informacji.
- Każde zapytanie niesie całą bazę, więc koszt wejścia rośnie razem z nią. To działa tylko przy małej bazie.
- Model dostaje też przepisy niezwiązane z pytaniem i ma je pomijać zgodnie z promptem.
- Anonimizacja regułami nie złapie nazwiska bez rozpoznanego imienia ani zwrotu „Pan/Pani”.
- Skanów PDF (bez warstwy tekstu) nie da się analizować.
- Jakość odpowiedzi nie była oceniona przez prawnika.

## Poza zakresem wczesnego demo

Plany z `Wizja.md`, na razie niezrobione:

- narzędzia dla modelu (function calling),
- generowanie poprawionej wersji umowy,
- pełne teksty ustaw i wyszukiwanie przez embeddingi,
- sprawdzanie w kodzie, czy powołane przepisy są w bazie.
