# LegAid

Asystent AI do analizy umów i pism prawnych według prawa polskiego.
Tworzenie agentów AI w .NET, PW. Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski.

Notatki do pokazu, razem około 9 minut. Komendy są w `DEMO.md`.

## Problem

Większość ludzi podpisuje umowę najmu albo regulamin siłowni bez czytania drobnego druku, a o prawnika nikt nie pyta. To samo z wezwaniem do zapłaty: list przychodzi, jest strach, a nie wiadomo, czy dług w ogóle jest jeszcze wymagalny.

LegAid dostaje pytanie albo tekst umowy czy pisma i odpowiada zwykłym językiem, z podaniem przepisu. Przy umowie mówi, które zapisy są problemem i jak je poprawić.

## Działający agent (3-4 min)

Pokaz w czacie pod `http://localhost:5080`, w tej kolejności:

1. Umowa najmu z trzema złymi zapisami (pytanie 2 z `DEMO.md`). Model wskazuje problemy i proponuje nowe brzmienie. Trwa to około 30 s, więc w tym czasie omawiamy schemat niżej.
2. Dopytanie „a co z § 7?”. Samo pytanie nic nie mówi, a agent wie, że chodzi o kaucję z poprzedniej wiadomości, bo pamięta rozmowę.
3. Pytanie spoza bazy: „Ile wynosi okres wypowiedzenia umowy o pracę po 4 latach zatrudnienia?”. Agent po kilku sekundach przyznaje, że nie ma informacji.

```
POST /api/agent/ask  { "message": "...", "history": [...] }
   -> AgentEndpoints   sprawdza, czy wiadomość nie jest pusta
   -> LegalAgent       składa rozmowę dla modelu
   -> KnowledgeBase    wszystkie przepisy z Knowledge/przepisy.txt
   -> IChatClient      instrukcje + przepisy + historia + pytanie
   -> OpenAI           gpt-6-luna
<- { "answer": "...", "model": "..." }
```

ASP.NET Core (.NET 10), Microsoft.Extensions.AI, OpenAI. Czat to jeden plik `wwwroot/index.html`. Historię trzyma przeglądarka, serwer niczego nie zapisuje.

Mówimy wprost: to na razie jeden agent bez narzędzi. Składa się z modelu, instrukcji, kontekstu i pamięci rozmowy.

## Dobór modelu (1,5 min)

Zaczęliśmy od `gpt-5-nano`, przeszliśmy na `gpt-6-luna`. Porównanie na 5 pytaniach z demo, po jednym przebiegu:

| | `gpt-5-nano` | `gpt-6-luna` |
|---|---|---|
| Czas odpowiedzi | 24–48 s | 7–18 s |
| Tokeny wyjścia | 4490–8628 | 486–1568 |
| Art. 483 KC przy karze za czynsz | Pominięty | Użyty poprawnie |
| Art. 385³ pkt 23 przy sądzie klubu | Pominięty | Użyty poprawnie |

Tani model wystarcza, bo źródłem wiedzy jest baza. Pytanie kosztuje szacunkowo około $0,001. Mocniejszych modeli nie testowaliśmy, więc mówimy, że Luna wystarcza na tym etapie, a nie że jest najlepsza.

Agent nie wie, z jakim modelem rozmawia, zna tylko `IChatClient`. Model zmienia się jedną linią w `appsettings.json`.

## Instrukcje (2 min)

W `Agents/LegalAgentPrompts.cs`.

- Rola i dwa tryby: przy umowie szuka zapisów niezgodnych z prawem, ryzykownych i luk, a przy innym piśmie wyjaśnia, co znaczy, jakie są terminy i co można zrobić.
- Opiera się tylko na przepisach z bazy, streszcza je własnymi słowami i podaje etykietę, np. `[KC art. 484]`.
- Gdy czegoś w bazie nie ma, pisze „Nie mam informacji na ten temat.”. O samej bazie nie wspomina.
- Używa wszystkich przepisów związanych ze sprawą, a o niepasujących nie pisze.
- Na końcu zaznacza, że to nie jest porada prawna.

Przykład iteracji: wcześniej model powoływał przepisy spoza bazy (art. 746 i 750 KC) i pomijał art. 385³ pkt 14 KC przy regulaminie siłowni. Po ograniczeniu do bazy używa pkt 14. Reguła „sprawdź każdy punkt” zaczęła jednak wyciekać do odpowiedzi jako zdanie o niepasujących przepisach, więc trzeba było ją przeformułować. Prompt trzeba testować na pytaniach tak jak kod.

Cena ostrożności: agent odpowiada na mniej pytań, ale każdą odpowiedź da się sprawdzić.

## Kontekst (2 min)

Model dostaje instrukcje, 20 artykułów z KC, KP, ustawy o ochronie praw lokatorów i ustawy o BIG, ostatnie 20 wiadomości i pytanie. Razem około 5500 tokenów na pytanie.

Bez bazy model odpowiada z pamięci i potrafi pomylić numer artykułu. Baza to plik tekstowy z przepisami dobranymi pod nasze przykłady.

Mieliśmy lokalne wyszukiwanie, ale je usunęliśmy, bo przy tak małej bazie szkodziło: przy wezwaniu do zapłaty gubiło art. 117 KC. Teraz model widzi całą bazę i używa art. 117 § 2¹ KC, kluczowego dla konsumenta.

Jakość odpowiedzi zależy wprost od kontekstu: po dodaniu art. 481 KC agent znów proponuje odsetki zamiast niedozwolonej kary za czynsz.

Pełne teksty ustaw w ten sposób się nie zmieszczą, więc wtedy dojdzie RAG z embeddingami.

## Ograniczenia i co dalej (30 s)

Ograniczenia:

- reguły wynikają tylko z promptu, kod ich nie wymusza
- odpowiedzi nie były oceniane przez prawnika
- baza jest mała i dobrana pod przykłady

Co dalej:

- odpowiedź w JSON (lista ryzyk zamiast tekstu)
- wgrywanie PDF i DOCX
- pełne teksty KC i KP w bazie i wyszukiwanie przez embeddingi
- kilku współpracujących agentów
- testy jakości odpowiedzi na zestawie przykładowych umów
