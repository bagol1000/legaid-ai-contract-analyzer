# LegAid – porównanie modeli AI

Stan na 5 listopada 2026

## Wniosek

Zostajemy przy **gpt-6-luna**. Spośród szybkich modeli tylko ona bezbłędnie rozpoznała niedopuszczalną karę umowną i najlepiej poradziła sobie z wezwaniem do zapłaty. Odpowiada średnio w 11 sekund i kosztuje około 8 zł za 1000 pytań.

Najlepszy model z PCSS to **gpt-oss_120b**: jest szybszy (7 s) i tańszy (5 zł), ale słabszy w trudniejszych pytaniach. Trzymamy go jako zapasowy model na wypadek, gdyby dane musiały zostać w Polsce.

Claude Sonnet 5.5 dorównuje lunie jakością, ale przy naszym zużyciu kosztuje około 146 zł za 1000 pytań, czyli mniej więcej 18 razy więcej.

## Jak testowaliśmy

Każdy model przeszedł przez naszą aplikację w tej samej konfiguracji: ten sam prompt systemowy i ta sama baza 20 przepisów. Dostał też te same sześć pytań:

1. Urlop w pierwszej pracy (Kodeks pracy)
2. Umowa najmu z karą 1000 zł za dzień spóźnienia, wypowiedzeniem „w każdym czasie” i bezzwrotną kaucją
3. Niezwrócona kaucja po wyprowadzce
4. Regulamin siłowni: jednostronna zmiana ceny, 24 miesiące bez wypowiedzenia, sąd właściwy dla klubu
5. Wezwanie do zapłaty za fakturę z 2021 r.
6. Testament i wydziedziczenie. Tego tematu nie ma w bazie, więc sprawdzamy, czy model nie zmyśla.

Ocenialiśmy trafność prawną, trzymanie się bazy i styl odpowiedzi. Do tego doszły czas i koszt. Koszt liczymy z tokenów zapisanych w logach i cennika dostawcy. Modele Claude (Haiku 5.5 i Sonnet 5.5) testowaliśmy osobnym skryptem przez oficjalne SDK Anthropic. Prompt i baza były identyczne jak w aplikacji, tylko zapytanie nie przechodziło przez nią. Każdy model miał jedno podejście, więc wyniki to dobra orientacja, nie twardy pomiar.

Testy można powtórzyć skryptem [`scripts/compare-models.ps1`](../scripts/compare-models.ps1).

## Wyniki

Tabela obejmuje tylko modele, które odpowiadały średnio w mniej niż 20 sekund. Oceny są w skali 1–5.

| # | Model | Dostawca | Prawo | Trzyma się bazy | Styl | Śr. czas | Koszt / 1000 pytań |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | gpt-6-luna | OpenAI | 5 | 5 | 5 | 10,8 s | ok. 8 zł |
| 2 | claude-sonnet-5-5 | Anthropic | 5 | 4 | 5 | 12,9 s | ok. 146 zł |
| 3 | gpt-oss_120b | PCSS | 4 | 5 | 4 | 6,8 s | 5 zł |
| 4 | MiMo-V2.6-Flash-RL | PCSS | 4 | 5 | 5 | 8,1 s | 11 zł |
| 5 | gemma-4-26B-A4B-it | PCSS | 4 | 5 | 4 | 4,0 s | 6 zł |
| 6 | claude-haiku-5-5 | Anthropic | 4 | 4 | 3 | 13,9 s | ok. 10 zł |
| 7 | DeepSeek-V4-Flash | PCSS | 4 | 3 | 5 | 8,3 s | 13 zł |
| 8 | Qwen3-VL-235B | PCSS | 4 | 5 | 4 | 8,7 s | 76 zł |
| 9 | MiniMax-M2.5 | PCSS | 4 | 5 | 2 | 10,4 s | 26 zł |
| 10 | Bielik-11B-v3.0-Instruct | PCSS | 3 | 4 | 4 | 5,1 s | 4 zł |
| 11 | Mistral-Small-3.2:24b | PCSS | 2 | 5 | 4 | 8,0 s | 4 zł |
| 12 | bielik_11b | PCSS | 2 | 4 | 4 | 10,7 s | 4 zł |

Koszt policzyliśmy ze średniego zużycia, czyli około 6–9 tys. tokenów wejścia na pytanie, głównie na bazę przepisów. Ceny PCSS pochodzą z ich panelu. Cenę gpt-6-luna ($0,20 / $1,00 za 1M tokenów) wzięliśmy z [pricepertoken.com](https://pricepertoken.com/pricing-page/model/openai-gpt-6-luna) Ceny Claude ($0,10 / $0,50 za Haiku 5.5 i $2 / $10 za Sonnet 5.5) pochodzą z cennika Anthropic. Kwoty w dolarach przeliczyliśmy po ok. 4 zł za dolara. Przy włączonym cache'owaniu promptu koszt Sonneta spadłby mniej więcej o połowę, ale nadal byłby mniej więcej 10 razy wyższy niż luny.

## Co odróżniło modele

Na łatwych pytaniach (urlop, kaucja, siłownia) prawie wszystkie odpowiadały dobrze. Różnice wyszły na trzech rzeczach:

- **Przedawnienie przy wezwaniu do zapłaty.** Dobre modele same zauważyły, że faktura z 2021 r. może być przedawniona. Radziły też poprosić o dokumenty, zanim cokolwiek się zapłaci. Mistral Small w ogóle nie wspomniał o przedawnieniu, a Bielik v3.0 odpowiedział ogólnikowo.
- **Kara umowna za czynsz.** Każdy model uznał karę 1000 zł dziennie za rażąco wygórowaną. Z szybkich modeli tylko gpt-6-luna i Claude Sonnet 5.5 zauważyły, że kary umownej w ogóle nie można zastrzec za spóźnienie z zapłatą pieniędzy (art. 483 KC). Starszy bielik_11b wręcz zaproponował obniżenie kary do 50 zł dziennie.
- **Pytanie spoza bazy.** Większość uczciwie odpowiedziała, że nie ma informacji. Najlepiej zrobił to MiMo-V2.6-Flash: odmówił i od razu odesłał do notariusza. DeepSeek-V4-Flash zaczął objaśniać prawo spadkowe spoza bazy, czyli złamał zasadę.

Jeden model miał też problem techniczny. MiniMax-M2.5 we wszystkich sześciu odpowiedziach pokazywał użytkownikowi swoje „myślenie” w znacznikach `<think>`. Bez dodatkowego filtrowania nie nadaje się do czatu.

Oba modele Claude piszą długo: Haiku średnio prawie 3 tys. tokenów na odpowiedź. Zdarza im się też wspomnieć, że „dysponują” tylko częścią przepisów, choć prompt każe o tym nie pisać. To drobiazg, ale widać go w odpowiedziach.

## Finał: gpt-6-luna kontra gpt-oss_120b

Luna wygrywa jakością przy podobnym koszcie. gpt-oss_120b jest szybszy, ale myli się tam, gdzie to najbardziej boli.

| | gpt-6-luna | gpt-oss_120b |
| --- | --- | --- |
| Kara umowna za czynsz | Od razu wskazuje, że jest niedopuszczalna (art. 483 KC), i proponuje odsetki | Uznaje ją tylko za wygórowaną, art. 483 cytuje w złym kontekście |
| Wezwanie do zapłaty | Przedawnienie, prośba o dokumenty i ostrzeżenie, żeby nie uznawać długu | Przedawnienie i prośba o dokumenty |
| Pytanie spoza bazy | Odmawia, nic nie zmyśla | Odmawia, nic nie zmyśla |
| Styl | Zwięzły, ostrożny, gotowe brzmienie poprawionych zapisów | Rzeczowy, ale często w szerokich tabelach |
| Śr. czas | 10,8 s | 6,8 s |
| Koszt / 1000 pytań | ok. 8 zł | 5 zł |

Za gpt-oss_120b przemawia jedno: działa na infrastrukturze PCSS w Polsce, więc treść umów nie trafia do OpenAI. Jeśli to będzie wymóg klienta, jest najlepszym zamiennikiem. Nasz kod obsługuje oba modele bez zmian, wystarczy inny endpoint i klucz.

## Co odpadło

Z zestawienia usunęliśmy modele, które odpowiadały średnio dłużej niż 20 sekund. Użytkownik czatu nie będzie tyle czekał.

| Model | Śr. czas | Uwagi |
| --- | --- | --- |
| GLM-5.1 | 47 s | Bardzo dobre odpowiedzi, wyłapał art. 483 KC. 110 zł / 1000 pytań |
| Qwen3.5-397B | 39 s | Bardzo dobre odpowiedzi, wyłapał art. 483 KC. 69 zł / 1000 pytań |
| GLM-5.3-Flash | 34 s | Najlepszy jakościowo spośród modeli PCSS. 45 zł / 1000 pytań |
| DeepSeek-V3.1 | 34 s | Poprawny, ale drogi: 125 zł / 1000 pytań |
| GLM-5.2 | ponad 90 s | Przekraczał limit czasu, najdroższy w ofercie |
| Qwen3.8-27B | ponad 100 s | Prawie każde pytanie kończyło się przekroczeniem limitu |

Dwa modele odpadły z innych powodów. MiMo-V2.5 na pytanie o wezwanie zwrócił pustą odpowiedź, bo cały limit 2048 tokenów zużył na „myślenie”. Pozycja pl1532-01 z listy PCSS nie jest modelem czatu i API ją odrzuca.

Jeśli kiedyś liczyć się będzie wyłącznie jakość, na przykład przy analizie długich umów w tle, warto wrócić do GLM-5.3-Flash.
