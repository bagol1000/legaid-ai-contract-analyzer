# Architektura

Kod w `src/LegAid.Api` jest podzielony na warstwy. Każda ma swój folder, a namespace odpowiada folderowi. Wszystko jest w jednym projekcie, bo przy tej wielkości osobne projekty nic by nie dały. Reguły zależności pilnujemy sami.

Uzasadnienia decyzji, także tej o podziale na warstwy (ADR-0005), są w [`adr/`](adr/README.md).

```
src/LegAid.Api/
├── Model/                 czym system operuje
│   ├── ChatTurn.cs            wiadomość rozmowy (rola jako enum)
│   ├── ContractReport.cs      raport ryzyk, zapis umowy, postęp analizy
│   ├── KnowledgeFragment.cs   jeden artykuł z bazy
│   ├── LegalAgentAnswer.cs    odpowiedź agenta
│   └── PersonalData.cs        rodzaje danych osobowych, wynik anonimizacji
├── Abstractions/          czego logika potrzebuje, bez „jak”
│   ├── IAnonymizer.cs         usuwanie danych osobowych
│   ├── IDocumentTextExtractor.cs  tekst z PDF i DOCX
│   ├── IKnowledgeSource.cs    źródło przepisów
│   └── ILegalAgent.cs
├── Logic/                 co system robi
│   ├── AgentMessages.cs       wspólny początek rozmowy agentów: instrukcje + przepisy
│   ├── LegalAgent.cs          agent czatu
│   ├── KnowledgeOptions.cs    włączanie i wyłączanie bazy
│   ├── ContractAnalysis/      analiza umowy przez kilku agentów
│   │   ├── ContractAnalyzer.cs    orkiestrator
│   │   ├── ContractAnalystAgent.cs, ClauseReviewerAgent.cs, ReportVerifierAgent.cs
│   │   ├── ClauseSplitter.cs      podział na paragrafy
│   │   └── RiskList.cs            format odpowiedzi agentów (JSON)
│   └── Prompts/               LegalAgentPrompts.cs, ContractAnalysisPrompts.cs
├── Infrastructure/        jak to technicznie zrobić
│   ├── OpenAIOptions.cs       klucz i model OpenAI
│   ├── Anonymization/         RegexAnonymizer.cs, PolishFirstNames.cs
│   ├── Documents/             DocumentTextExtractor.cs (PdfPig, OpenXml)
│   └── Knowledge/
│       ├── FileKnowledgeSource.cs   IKnowledgeSource z pliku
│       ├── KnowledgeFileParser.cs   format przepisy.txt -> artykuły
│       └── przepisy.txt
├── Presentation/          jak świat rozmawia z systemem
│   └── Endpoints/
│       ├── AgentEndpoints.cs      czat: /api/agent/ask i /ask/stream
│       ├── ContractEndpoints.cs   POST /api/contract/analyze
│       ├── ServerSentEventStream.cs  wspólne strumieniowanie SSE
│       └── AgentDtos.cs, ContractDtos.cs  kształt JSON-a w API
├── wwwroot/index.html     czat z raportem (prezentacja)
└── Program.cs             composition root: skleja warstwy przez DI
```

Testy są w `tests/LegAid.Api.Tests` (jednostkowe i całego API z udawanym modelem), a ewaluacja na prawdziwym modelu w `tests/LegAid.Eval`.

## Analiza umowy

```
PDF/DOCX → DocumentTextExtractor → RegexAnonymizer → ClauseSplitter
         → analityk (cała umowa: rodzaj, streszczenie, luki)
         → recenzenci (każdy paragraf osobno, do 4 naraz)
         → weryfikator (usuwa niepotwierdzone i duplikaty)
         → raport, na bieżąco przez SSE
```

Każdy agent to zwykła klasa z `IChatClient`, a odpowiedź dostaje jako JSON według schematu. Kolejnością steruje kod w `ContractAnalyzer`, a nie model.

## Warstwy

| Warstwa | Odpowiada za | Nie może zależeć od |
|---|---|---|
| Model | pojęcia domeny: wiadomość, przepis, odpowiedź | niczego |
| Abstractions | interfejsy, których potrzebuje logika | Logic, Infrastructure, Presentation |
| Logic | działanie agentów: historia, baza w kontekście, prompty, orkiestracja | Infrastructure, Presentation |
| Infrastructure | plik z przepisami, anonimizacja, odczyt dokumentów, konfiguracja OpenAI | Logic, Presentation |
| Presentation | HTTP, walidacja żądań, DTO, czat w przeglądarce | Infrastructure |

Zależności idą do środka:

```
Presentation ──► Logic ──► Abstractions ◄── Infrastructure
      └────────────┴──────► Model ◄──────────────┘
```

Tylko `Program.cs` zna wszystkie warstwy. Tam jest wybierana implementacja każdego interfejsu.

## Zasady

- **Wejście do systemu to prezentacja, wyjście to infrastruktura.** Nasze API i czat są w `Presentation`, a wywołania OpenAI i czytanie pliku w infrastrukturze.
- **Logika zna tylko interfejsy.** `LegalAgent` dostaje `IKnowledgeSource` i `IChatClient` (interfejs z biblioteki Microsoft.Extensions.AI). Plik z przepisami można zamienić na bazę wektorową, a OpenAI na model z PCSS. Wtedy zmienia się tylko `Program.cs` i dochodzi nowa klasa w `Infrastructure`.
- **API ma własne DTO.** `ChatTurnDto` z rolą jako tekst `"user"` lub `"assistant"` to format JSON-a, a `ChatTurn` z enumem to model, z którym pracuje agent. Endpoint tłumaczy jedno na drugie, więc zmiana formatu API nie zmienia logiki.
- **Interfejs tylko tam, gdzie coś realnie się wymienia** albo potrzebna jest atrapa do testów. Nie robimy interfejsu do każdej klasy.

## Odstępstwa

- `wwwroot/` jest w korzeniu projektu, a nie w `Presentation/`, bo ASP.NET Core domyślnie serwuje i publikuje pliki statyczne właśnie stamtąd.
- `KnowledgeOptions` jest w `Logic`, bo decyduje o działaniu agenta. Wartość pochodzi z `appsettings.json` i ładuje ją `Program.cs`.
