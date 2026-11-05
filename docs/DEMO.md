# Demo

## Przed zajęciami

Klucz OpenAI ustawiamy raz (w `src\LegAid.Api`):

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
```

Albo tylko na bieżące okno: `$env:OpenAI__ApiKey = "sk-..."`.

Dzień wcześniej puszczamy wszystkie zapytania z listy niżej i zapisujemy jedną dobrą odpowiedź, na wypadek gdyby na zajęciach nie było sieci.

## Na zajęciach

1. Okno 1, serwer:

   ```powershell
   cd src\LegAid.Api
   dotnet run
   ```

   Czekamy na `Now listening on: http://localhost:5080`. Wyżej powinno być `Baza wiedzy: wczytano 20 przepisów`.

2. Okno 2: wklejamy zapytania po kolei.

3. Po każdej odpowiedzi pokazujemy powołane przepisy, np. `[KP art. 153]`. Agent wysyła do modelu całą bazę, więc czy przepis z niej pochodzi, sprawdzamy w `Knowledge/przepisy.txt`.

4. Na koniec pokazujemy kod (lista na dole).

Odpowiedź przychodzi po kilkunastu sekundach.

## Zapytania

### 1. Urlop w pierwszej pracy

Szef nie ma racji: urlop narasta co miesiąc po 1/12 (art. 153 KP), a roczny wymiar to 20 albo 26 dni (art. 154 KP).

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Zaczynam pierwszą pracę i szef mówi, że w pierwszym roku urlop mi się nie należy. Czy to prawda?"}')) -TimeoutSec 120).answer
```

### 2. Umowa najmu

Kary umownej nie można zastrzec za spóźnienie z zapłatą, tylko za zobowiązania niepieniężne (art. 483 KC). Kaucja musi wrócić do najemcy (art. 6 ustawy o lokatorach), a wynajmujący może wypowiedzieć umowę tylko z powodów z ustawy (art. 11).

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Mam podpisać umowę najmu mieszkania i niepokoją mnie te zapisy: § 5. Za każdy dzień spóźnienia z czynszem najemca płaci 1000 zł kary umownej. § 6. Wynajmujący może wypowiedzieć umowę w każdym czasie. § 7. Kaucja nie podlega zwrotowi. Podpisywać?"}')) -TimeoutSec 120).answer
```

### 3. Kaucja po wyprowadzce

Właściciel ma miesiąc na zwrot kaucji (art. 6 ustawy o lokatorach), więc już jest spóźniony.

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Wyprowadziłem się z wynajmowanego mieszkania dwa miesiące temu, a właściciel dalej nie oddał mi kaucji. Może ją tak po prostu zatrzymać?"}')) -TimeoutSec 120).answer
```

### 4. Regulamin siłowni

Wszystkie trzy zapisy są w katalogu klauzul niedozwolonych (art. 385³ KC).

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Zapisałem się na siłownię i w regulaminie jest: § 4. Klub może w każdym czasie zmienić cenę karnetu. § 7. Klubowicz nie może wypowiedzieć umowy przed upływem 24 miesięcy. § 9. Spory rozstrzyga sąd właściwy dla siedziby Klubu. Czy to w ogóle jest legalne?"}')) -TimeoutSec 120).answer
```

### 5. Wezwanie do zapłaty

Sama faktura z 2021 r. nie przesądza o przedawnieniu, bo termin zależy od rodzaju należności: 2 lata przy sprzedaży w ramach działalności (art. 554 KC), a poza tym 3 albo 6 lat (art. 118 KC). Po przedawnieniu nie można domagać się zapłaty od konsumenta (art. 117 § 2¹ KC).

```powershell
(Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes('{"message":"Przyszedł do mnie list: Wzywamy do zapłaty 2 340 zł z faktury nr 14/2021 w terminie 7 dni, inaczej sprawa trafi do sądu. Nawet nie pamiętam, za co to było. Muszę płacić?"}')) -TimeoutSec 120).answer
```

### 6. Pusta wiadomość

Błąd 400, zapytanie nawet nie trafia do modelu.

```powershell
Invoke-RestMethod http://localhost:5080/api/agent/ask -Method Post -ContentType "application/json" -Body '{"message":""}'
```

Te same zapytania są w `src/LegAid.Api/LegAid.Api.http`, można je też puszczać z VS / Ridera.

## Kod do pokazania

- `Program.cs`: rejestracja `IChatClient` (OpenAI) i bazy wiedzy, klucz spoza repo
- `Agents/LegalAgent.cs`: dokleja bazę przepisów do rozmowy, woła model
- `Agents/KnowledgeBase.cs`: wczytuje przepisy i dzieli je na artykuły
- `Knowledge/przepisy.txt`: baza, 20 artykułów wyciętych z KC, KP i dwóch ustaw
- `Agents/LegalAgentPrompts.cs`: prompt

## Jak coś nie działa

- Serwer nie wstaje: brak klucza, komunikat mówi, co ustawić.
- Zapytanie zwraca 500: przyczyna jest w oknie 1 (zły klucz, brak środków na koncie OpenAI).
- Baza wiedzy przeszkadza: w `appsettings.json` ustawiamy `"Knowledge": { "Enabled": false }` i restartujemy serwer. Agent odpowiada wtedy tylko z wiedzy modelu.
