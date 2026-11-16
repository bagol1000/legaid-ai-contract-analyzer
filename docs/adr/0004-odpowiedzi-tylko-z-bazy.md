# ADR-0004: Odpowiedzi wyłącznie na podstawie bazy przepisów

- Status: Zaakceptowana
- Data: 2026-11-05
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

Celem wyspecjalizowanego agenta jest opieranie się na sprawdzonym prawie, a nie na wiedzy modelu z internetu, która może być nieaktualna. W testach model z własnej wiedzy powoływał przepisy spoza bazy.

## Rozważane opcje

1. **Tylko wiedza modelu:** nie da się sprawdzić, skąd są przepisy.
2. **Baza i wiedza modelu:** nie wiadomo, co pochodzi z którego źródła.
3. **Tylko baza:** gdy czegoś w niej nie ma, agent pisze „Nie mam informacji na ten temat.”.

## Decyzja

Agent odpowiada wyłącznie na podstawie bazy. Reguły są w prompcie (`Logic/Prompts/LegalAgentPrompts.cs`).

## Konsekwencje

- Każdy powołany przepis można sprawdzić w `przepisy.txt`.
- Agent odpowiada na mniej pytań.
- Regułę wymusza tylko prompt, więc model może ją czasem złamać.
