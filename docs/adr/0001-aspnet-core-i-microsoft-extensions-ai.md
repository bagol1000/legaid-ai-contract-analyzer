# ADR-0001: ASP.NET Core i Microsoft.Extensions.AI

- Status: Zaakceptowana
- Data: 2026-11-02
- Decydenci: Adam Bagiński, Krzysztof Dąbrowski, Adam Drogowski

## Kontekst

Potrzebujemy backendu, który przekazuje pytania do modelu językowego. .NET zaproponował prowadzący, a my chcemy przy okazji nauczyć się C#.

## Rozważane opcje

1. **Python** (np. FastAPI z LangChain): najpopularniejszy w AI, ale nie uczy C#.
2. **C#: ASP.NET Core (.NET 10) i Microsoft.Extensions.AI.**

## Decyzja

Piszemy w C# na ASP.NET Core, a z modelem rozmawiamy przez `IChatClient` z Microsoft.Extensions.AI.

## Konsekwencje

- Uczymy się C# na realnym projekcie.
- Agent nie zależy od dostawcy modelu, więc model zmienia się w `appsettings.json`.
- Gotowych przykładów do agentów jest w .NET mniej niż w Pythonie.
