# Analiza i wybór agenta AI wspomagającego proces tworzenia systemu Investe

## 1. Analiza dostępnych narzędzi AI

Na rynku dostępnych jest kilka wiodących agentów AI wspomagających programowanie. Przed wyborem narzędzia przeprowadzono analizę porównawczą najważniejszych rozwiązań:

| Kryterium | GitHub Copilot | Cursor AI | Amazon CodeWhisperer | Tabnine |
|---|---|---|---|---|
| Integracja z VS Code / Visual Studio | ✅ natywna | ✅ własny edytor | ⚠️ plugin | ⚠️ plugin |
| Integracja z CLI | ✅ Copilot CLI | ❌ | ❌ | ❌ |
| Tryb agentowy (Agent Mode) | ✅ | ✅ | ❌ | ❌ |
| Dostępność dla studentów (bezpłatnie) | ✅ GitHub Education | ❌ | ⚠️ free tier | ⚠️ free tier |
| Obsługa C# / .NET | ✅ pełna | ✅ | ⚠️ ograniczona | ✅ |
| Obsługa Angular / TypeScript | ✅ pełna | ✅ | ⚠️ ograniczona | ✅ |
| Kontekst całego repozytorium | ✅ (Agent Mode) | ✅ | ❌ | ❌ |
| Praca z terminalem | ✅ Copilot CLI | ❌ | ❌ | ❌ |

### Dlaczego nie inne narzędzia?

**Cursor AI** — mimo zaawansowanego trybu agentowego wymaga przejścia na własny, dedykowany edytor. Dla projektu akademickiego z ustalonym stosem technologicznym (Visual Studio dla backendu .NET, VS Code dla Angular) oznaczałoby to podział środowisk lub porzucenie jednego z nich.

**Amazon CodeWhisperer** — brak natywnego trybu agentowego, słabsza obsługa .NET i brak integracji z terminalem. Narzędzie zoptymalizowane głównie pod ekosystem AWS, który nie jest używany w projekcie.

**Tabnine** — dobre uzupełnianie kodu, ale ograniczone do autouzupełniania. Brak trybu agentowego, brak CLI, brak możliwości prowadzenia rozmowy kontekstowej z całym projektem.

---

## 2. Wybór agenta AI — GitHub Copilot

### 2.1 Uzasadnienie wyboru

Wybrano **GitHub Copilot** w ramach programu **GitHub Education (GitHub Student Developer Pack)**, który zapewnia bezpłatny dostęp do pełnej wersji narzędzia przez czas trwania studiów.

Jest to optymalne rozwiązanie dla projektu Investe z następujących powodów:

**Pełna integracja ze środowiskiem pracy** — projekt składa się z dwóch oddzielnych aplikacji: backendu w .NET (Visual Studio) i frontendu w Angular (VS Code). GitHub Copilot posiada natywne rozszerzenia dla obu tych środowisk, co eliminuje konieczność przełączania się między narzędziami.

**Tryb agentowy (Agent Mode)** — Agent Mode w rozszerzeniu VS Code pozwala Copilotowi rozumieć kontekst całego repozytorium, a nie tylko otwartego pliku. Dzięki temu może samodzielnie proponować zmiany w wielu plikach jednocześnie, co jest szczególnie przydatne przy implementacji funkcjonalności przechodzących przez wszystkie warstwy (Controller → Service → Repository → DbContext).

**GitHub Copilot CLI** — umożliwia korzystanie z asystenta bezpośrednio w terminalu. Przydatne przy generowaniu komend Entity Framework (`dotnet ef migrations add`), Angular CLI (`ng generate component`), pracy z Git oraz debugowaniu błędów kompilacji.

**Spójność ekosystemu** — projekt jest hostowany na GitHubie, co zapewnia naturalną integrację: Copilot ma dostęp do historii commitów, pull requestów i Issues bezpośrednio w kontekście pracy.

### 2.2 Wybór modeli

GitHub Copilot udostępnia różne modele językowe, które są dobierane do złożoności zadania:

**GPT Codex (model zaawansowany)** — używany do zadań wymagających głębokiego rozumienia architektury i kontekstu całego projektu:
- generowanie kompletnych klas domenowych z relacjami EF Core
- implementacja warstwy serwisów z logiką biznesową (obliczenia P&L, średnia ważona)
- tworzenie konfiguracji JWT i ASP.NET Identity
- implementacja `IHostedService` dla background joba CoinGecko
- generowanie złożonych zapytań LINQ w repozytoriach
- tworzenie komponentów Angular z serwisami, interceptorami i guardami

**GPT mini (model lekki)** — używany do powtarzalnych, dobrze zdefiniowanych zadań:
- autouzupełnianie prostych właściwości i konstruktorów
- generowanie DTOs i modeli mapowania
- tworzenie migracji EF Core na podstawie gotowego modelu
- uzupełnianie metod CRUD w kontrolerach
- generowanie plików konfiguracyjnych (`appsettings.json`, `environment.ts`)
- poprawianie błędów kompilacji i literówek

Podział ten optymalizuje czas odpowiedzi — prostsza warstwa generuje wyniki natychmiastowo, podczas gdy zaawansowany model jest angażowany tylko gdy zadanie tego wymaga.

---

## 3. Metodologia pracy z agentem AI

### 3.1 Wybrana metodologia — Iterative Prompt Engineering z kontekstem architektonicznym

Zamiast traktować Copilota jako narzędzie do jednorazowego generowania kodu, przyjęto metodologię pracy iteracyjnej opartą na następujących zasadach:

**Zasada 1 — Kontekst przed kodem**
Przed każdą sesją pracy agent jest wprowadzany w kontekst zadania. Oznacza to, że przed wygenerowaniem np. serwisu `PortfolioService`, Copilotowi dostarczany jest schemat bazy danych, interfejs repozytorium i DTO, które mają być użyte. Agent nie generuje kodu w próżni.

**Zasada 2 — Jeden komponent na raz**
Każda sesja z agentem obejmuje jedną, dobrze zdefiniowaną jednostkę pracy: jeden serwis, jeden komponent Angular, jedna migracja. Unika się proszenia o wygenerowanie całej warstwy na raz, co zwiększa jakość i możliwość weryfikacji wyników.

**Zasada 3 — Weryfikacja przed akceptacją**
Kod wygenerowany przez agenta jest zawsze przeglądany przez dewelopera przed zatwierdzeniem. Copilot może popełniać błędy w logice biznesowej (np. niepoprawne obliczenie średniej ważonej ceny zakupu). Agent generuje — człowiek weryfikuje i decyduje.

**Zasada 4 — Stopniowe rozszerzanie**
Implementacja przebiega warstwami, od dołu do góry: najpierw model domenowy i migracje, potem repozytoria, następnie serwisy, na końcu kontrolery i frontend. Dzięki temu agent ma zawsze dostęp do gotowych niższych warstw jako kontekstu dla warstwy wyższej.

### 3.2 Podział zadań między tryby Copilota

| Tryb | Zastosowanie w projekcie |
|---|---|
| **Agent Mode (VS Code)** | Implementacja funkcjonalności przechodzących przez wiele plików, np. dodanie nowej encji wraz z repozytorium, serwisem i kontrolerem |
| **Inline Chat (edytor)** | Dopytywanie o konkretną metodę, refaktoryzacja wybranego fragmentu, wyjaśnianie istniejącego kodu |
| **Copilot CLI** | Generowanie komend EF Core, Angular CLI, Git; debugowanie błędów w terminalu |
| **Autouzupełnianie** | Szybkie uzupełnianie powtarzalnych fragmentów: właściwości, konstruktory, proste metody CRUD |

### 3.3 Przykładowy przepływ pracy (na przykładzie modułu Transakcji)

```
1. [Agent Mode] "Na podstawie klasy Transaction i interfejsu ITransactionRepository
   wygeneruj implementację TransactionRepository używającą AppDbContext"
        ↓
2. [Weryfikacja] przegląd kodu, sprawdzenie zapytań LINQ, korekta jeśli potrzeba
        ↓
3. [Agent Mode] "Wygeneruj ITransactionService i TransactionService implementujący
   logikę dodania transakcji kupna z aktualizacją Holding (średnia ważona)"
        ↓
4. [Inline Chat] "Jak działa ta formuła średniej ważonej? Czy jest poprawna?"
        ↓
5. [Autouzupełnianie] uzupełnienie TransactionController na podstawie wzorca z WalletController
        ↓
6. [CLI] "Jak dodać migrację EF Core po zmianie modelu Transaction?"
        ↓
7. [Agent Mode] "Wygeneruj komponent Angular TransactionFormComponent z reaktywnym
   formularzem i wywołaniem TransactionService.create()"
```

---

## 4. Specyfikacja systemu zgodna z metodologią

### 4.1 Podział pracy na sesje agentowe

Praca z agentem jest podzielona na etapy odpowiadające kolejnym warstwom systemu. Każdy etap kończy się weryfikacją przed przejściem do kolejnego.

**Etap I — Model domenowy i baza danych**
- generowanie klas encji (ApplicationUser, Wallet, Holding, Transaction, CoinPriceCache, PriceAlert)
- konfiguracja relacji w EF Core (`OnModelCreating`)
- generowanie i aplikowanie migracji

**Etap II — Warstwa repozytoriów**
- generowanie interfejsów repozytoriów
- implementacje korzystające z AppDbContext
- testy poprawności zapytań LINQ

**Etap III — Warstwa serwisów**
- implementacja logiki biznesowej: obliczenia P&L, średnia ważona, walidacja właściciela portfela
- integracja z CoinGeckoClient
- implementacja PriceBackgroundService

**Etap IV — Warstwa API**
- generowanie kontrolerów REST
- konfiguracja JWT i ASP.NET Identity
- middleware, CORS, Swagger

**Etap V — Frontend Angular**
- generowanie serwisów HTTP (`WalletService`, `TransactionService`, itp.)
- implementacja interceptora JWT
- generowanie komponentów widoków
- konfiguracja routingu z guardami

### 4.2 Prompty wzorcowe dla kluczowych zadań

Poniżej zdefiniowano szablony promptów dla najbardziej złożonych zadań w projekcie, gotowe do użycia w Agent Mode:

**Generowanie serwisu z logiką biznesową:**
> "Zaimplementuj `PortfolioService` implementujący `IPortfolioService`. Metoda `GetSummaryAsync(userId)` powinna pobrać wszystkie holdings użytkownika przez `IHoldingRepository`, dla każdego holdingu pobrać aktualną cenę z `IPriceService`, obliczyć aktualną wartość (`quantity × currentPrice`) oraz zysk/stratę (`(currentPrice - averageBuyPrice) × quantity`). Zwróć `PortfolioSummaryDto` z łączną wartością portfela i listą `HoldingDto`."

**Generowanie komponentu Angular:**
> "Wygeneruj `PortfolioSummaryComponent` w Angular 17 jako standalone component. Komponent ma wstrzykiwać `PortfolioService`, pobierać dane przez `getPortfolioSummary()` przy inicjalizacji i wyświetlać łączną wartość portfela oraz listę aktywów w tabeli. Użyj `AsyncPipe` i obsłuż stan ładowania."

**Generowanie background joba:**
> "Zaimplementuj `PriceBackgroundService` dziedziczący po `BackgroundService`. W metodzie `ExecuteAsync` co 5 minut wywołuj `IPriceService.RefreshCacheAsync()`. Użyj `IServiceScopeFactory` do pobrania serwisu ze scope w hoście."

---

## 5. Podsumowanie

GitHub Copilot z programu GitHub Education jest optymalnym wyborem dla projektu Investe ze względu na:

- **bezpłatny dostęp** przez cały czas trwania studiów bez ograniczeń funkcjonalnych
- **natywną integrację** z obydwoma środowiskami używanymi w projekcie (Visual Studio, VS Code)
- **Agent Mode** umożliwiający pracę na poziomie całego repozytorium, nie pojedynczego pliku
- **Copilot CLI** usprawniający pracę z terminalem (EF Core, Angular CLI, Git)
- **dwumodelowe podejście** (Codex / mini) optymalizujące szybkość i jakość w zależności od złożoności zadania

Przyjęta metodologia iteracyjna z zasadą kontekstu przed kodem i weryfikacji przed akceptacją zapewnia, że agent AI jest narzędziem wspomagającym dewelopera — nie zastępującym jego decyzji projektowych i architektonicznych.