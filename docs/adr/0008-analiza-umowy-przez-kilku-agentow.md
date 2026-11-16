# ADR-0008: Analiza umowy przez kilku agentów sterowanych kodem

- Status: Zaakceptowana
- Data: 2026-11-16
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

Jeden agent analizował całą umowę naraz i odpowiadał tekstem. Chcemy dokładniejszej analizy długich umów i raportu ryzyk zamiast tekstu.

## Rozważane opcje

1. **Jeden agent i raport w JSON:** prosto, ale długa umowa w jednym zapytaniu.
2. **Kilku agentów, kolejnością steruje model:** elastycznie, ale mniej przewidywalnie i drożej.
3. **Kilku agentów, kolejnością steruje kod.**

## Decyzja

Analityk czyta całą umowę, recenzenci oceniają paragrafy równolegle, a weryfikator sprawdza wynik. Kolejnością steruje `ContractAnalyzer`, a agenci odpowiadają w JSON według schematu. Analiza działa dla wgranych plików, a pytania idą do zwykłego czatu.

## Konsekwencje

- Każdy paragraf dostaje osobną ocenę, a czat pokazuje raport i postęp.
- Więcej wywołań modelu: przy 10 paragrafach 12 zapytań i ok. minuty czekania.
