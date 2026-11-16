# ADR-0005: Podział kodu na warstwy w folderach jednego projektu

- Status: Zaakceptowana
- Data: 2026-11-16
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

Kod nie miał jasnego podziału, co gdzie należy. Prowadzący zalecił podział na warstwy, ale bez skomplikowanej architektury.

## Rozważane opcje

1. **Zostawić jak jest.**
2. **Warstwy jako foldery w jednym projekcie.**
3. **Warstwy jako osobne projekty:** przy tej wielkości kodu to przesada.

## Decyzja

Dzielimy kod na foldery `Model`, `Abstractions`, `Logic`, `Infrastructure` i `Presentation`. To porządek w kodzie i dobra praktyka. Szczegóły są w [`ARCHITEKTURA.md`](../ARCHITEKTURA.md).

## Konsekwencje

- Wiadomo, gdzie co leży, a plik z przepisami czy model można wymienić bez zmian w logice.
- Reguł zależności pilnujemy sami, kompilator ich nie sprawdza.
