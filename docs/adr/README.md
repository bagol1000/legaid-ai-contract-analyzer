# Decyzje architektoniczne (ADR)

Każda ważna decyzja ma osobny plik. Dlaczego tak, opisuje [ADR-0000](0000-zapisujemy-decyzje-jako-adr.md).

| ADR | Decyzja | Status |
|---|---|---|
| [0000](0000-zapisujemy-decyzje-jako-adr.md) | Zapisujemy decyzje architektoniczne jako ADR-y | Zaakceptowana |
| [0001](0001-aspnet-core-i-microsoft-extensions-ai.md) | ASP.NET Core i Microsoft.Extensions.AI | Zaakceptowana |
| [0002](0002-model-gpt-6-luna.md) | Model gpt-6-luna, gpt-oss_120b jako zapasowy | Zaakceptowana |
| [0003](0003-mala-baza-przepisow-w-kontekscie.md) | Mała baza przepisów w pliku tekstowym, cała w kontekście | Zaakceptowana |
| [0004](0004-odpowiedzi-tylko-z-bazy.md) | Odpowiedzi wyłącznie na podstawie bazy przepisów | Zaakceptowana |
| [0005](0005-podzial-na-warstwy.md) | Podział kodu na warstwy w folderach jednego projektu | Zaakceptowana |
| [0006](0006-streamowanie-przez-sse.md) | Streamowanie odpowiedzi przez Server-Sent Events | Zaakceptowana |
| [0007](0007-anonimizacja-lokalna.md) | Anonimizacja lokalnie, regułami | Zaakceptowana |
| [0008](0008-analiza-umowy-przez-kilku-agentow.md) | Analiza umowy przez kilku agentów sterowanych kodem | Zaakceptowana |

## Nowy ADR

Kolejny numer, plik `NNNN-krotki-tytul.md`, sekcje:

```markdown
# ADR-NNNN: Tytuł w formie decyzji

- Status: Proponowana | Zaakceptowana | Odrzucona | Zastąpiona przez ADR-XXXX
- Data: RRRR-MM-DD
- Decydenci: ...

## Kontekst
## Rozważane opcje
## Decyzja
## Konsekwencje
```
