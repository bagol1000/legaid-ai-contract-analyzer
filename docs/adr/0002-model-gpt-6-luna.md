# ADR-0002: Model gpt-6-luna, gpt-oss_120b jako zapasowy

- Status: Zaakceptowana
- Data: 2026-11-05
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

Startowy `gpt-5-nano` był wolny (24–48 s) i pomijał kluczowe przepisy. Przetestowaliśmy kilkanaście modeli na pytaniach z demo ([`porownanie-modeli.md`](../porownanie-modeli.md)).

## Rozważane opcje

1. **gpt-6-luna:** 10,8 s, ok. 8 zł za 1000 pytań, najlepsza jakość wśród szybkich modeli.
2. **Claude Sonnet 5.5:** podobna jakość, ale ok. 18 razy drożej.
3. **gpt-oss_120b (PCSS):** szybszy i tańszy, ale słabszy. Dane zostają w Polsce.

## Decyzja

Używamy `gpt-6-luna`. `gpt-oss_120b` to zapas na wypadek, gdyby dane musiały zostać w Polsce.

## Konsekwencje

- Dobra jakość przy niskim koszcie.
- Treść umów trafia do OpenAI.
- Wyniki pochodzą z jednego przebiegu na model, więc są orientacyjne.
