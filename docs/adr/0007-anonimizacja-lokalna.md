# ADR-0007: Anonimizacja lokalnie, regułami

- Status: Zaakceptowana
- Data: 2026-11-16
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

Umowy zawierają dane osobowe, a tekst trafia do OpenAI. Dane trzeba usunąć, zanim opuszczą serwer.

## Rozważane opcje

1. **Reguły na naszym serwerze:** sumy kontrolne numerów, wzorce adresów i telefonów, lista imion.
2. **Model językowy (np. w PCSS):** lepiej rozpoznaje nazwiska, ale dane i tak wychodzą z serwera.

## Decyzja

Anonimizujemy lokalnie, regułami (`RegexAnonymizer`), wszystko, co trafia do modelu: wgrane umowy i wiadomości z czatu. Dane zastępują znaczniki, np. [OSOBA_1].

## Konsekwencje

- Dane osobowe nie opuszczają serwera.
- Nazwisko bez imienia z listy albo bez „Pan/Pani” przejdzie.
