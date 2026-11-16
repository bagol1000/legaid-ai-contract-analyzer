# ADR-0003: Mała baza przepisów w pliku tekstowym, cała w kontekście

- Status: Zaakceptowana
- Data: 2026-11-05
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

To wersja demo, więc na test wystarczy kilka przepisów pod przykładowe pytania. Pierwsza wersja wybierała przepisy wyszukiwaniem po słowach i przy wezwaniu do zapłaty gubiła art. 117 KC.

## Rozważane opcje

1. **Pełne teksty ustaw i RAG:** docelowo tak, ale na demo za dużo pracy.
2. **Mały plik i wyszukiwanie po słowach:** gubiło ważne przepisy.
3. **Mały plik, cały w każdym zapytaniu.**

## Decyzja

Trzymamy 20 artykułów w pliku `Infrastructure/Knowledge/przepisy.txt` i wysyłamy całą bazę w każdym zapytaniu. To wersja demo, więc nie było sensu tego rozdrabniać.

## Konsekwencje

- Prosty kod, a model widzi każdy przepis.
- Koszt zapytania rośnie razem z bazą, więc przy pełnych ustawach trzeba będzie przejść na RAG.
