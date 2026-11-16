# ADR-0006: Streamowanie odpowiedzi przez Server-Sent Events

- Status: Zaakceptowana
- Data: 2026-11-16
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

Użytkownik czekał kilkanaście sekund na całą odpowiedź naraz. Chcemy pokazywać tekst na bieżąco, w miarę jak model go generuje.

## Rozważane opcje

1. **WebSocket (np. SignalR):** dwukierunkowy, ale nam wystarczy przesył od serwera do klienta.
2. **Server-Sent Events (SSE):** jednokierunkowy strumień przez zwykłe HTTP, wbudowany w ASP.NET Core.

## Decyzja

Endpoint `POST /api/agent/ask/stream` wysyła odpowiedź jako SSE, a czat dopisuje tekst na bieżąco. Endpoint `/ask` zostaje bez zmian.

## Konsekwencje

- Pierwsze słowa widać po kilku sekundach zamiast po całej odpowiedzi.
- Błąd modelu w trakcie odpowiedzi przychodzi jako zdarzenie `error`, bo status 200 jest już wysłany.
