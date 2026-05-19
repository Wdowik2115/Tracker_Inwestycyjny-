Ten plik jest wspólnym kontekstem projektu dla narzędzi AI: *GitHub Copilot CLI*, *OpenCode* i *Gemini CLI*.
Przed generowaniem kodu przeczytaj cały plik. Przestrzegaj opisanych tu konwencji i wzorców.

-----

## Nowe Funkcjonalności (Moja Sekcja)

### Zarządzanie Profilem i Bezpieczeństwem Konta

Ta funkcjonalność umożliwia użytkownikom pełne zarządzanie swoimi danymi osobowymi oraz ustawieniami bezpieczeństwa.

**Główne cechy:**
- Aktualizacja danych profilowych (Imię, Nazwisko).
- Zmiana hasła z walidacją obecnego hasła.
- Zarządzanie preferencjami aplikacji (np. domyślna waluta wyświetlania).
- Ulepszony system logowania z obsługą błędów i feedbackiem UI.

**Architektura:**
- `UserController` - endpointy do pobierania i aktualizacji profilu.
- `UserService` - logika biznesowa zmiany danych i haseł.
- `SettingsComponent` (Angular) - kompletny formularz zarządzania profilem.

-----

## Projekt

*Investee* to aplikacja webowa do śledzenia inwestycji kryptowalutowych.

- Frontend: *Angular 17* (SPA, standalone components)
- Backend: *ASP.NET Core Web API (.NET 8)*, architektura warstwowa
- Baza danych: *MS SQL Server* + Entity Framework Core
- Uwierzytelnianie: *ASP.NET Identity + JWT*
- Zewnętrzne API: *CoinGecko* (darmowy plan, ~30 req/min)

Repozytorium składa się z dwóch projektów:

investee/
├── investee-client/     # Angular 17
└── investee-api/        # ASP.NET Core Web API

-----

## Architektura backendu

Ścisła architektura warstwowa. Każda warstwa zna tylko warstwę bezpośrednio poniżej.

Controllers  →  Services (interfejsy)  →  Repositories (interfejsy)  →  AppDbContext  →  SQL Server
                                                                     ↗
                                          CoinGeckoClient  ─────────

### Zasady które zawsze obowiązują

- Kontrolery *nie zawierają logiki biznesowej* — tylko odbierają żądanie, wywołują serwis, zwracają odpowiedź.
- Serwisy *nie piszą LINQ* bezpośrednio — idą przez interfejs repozytorium.
- Repozytoria *nie znają DTOs* — operują wyłącznie na encjach.
- Wszystkie zależności są wstrzykiwane przez *konstruktor* (DI).
- Każdy interfejs ma dokładnie jedną implementację (nie stosujemy wielu implementacji jednego interfejsu).

-----

## Model domenowy (encje)

ApplicationUser   // dziedziczy po IdentityUser
Wallet            // należy do ApplicationUser (UserId)
Holding           // należy do Wallet; jedna kryptowaluta = jeden Holding
Transaction       // należy do Wallet; Type = Buy | Sell
CoinPriceCache    // cache cen z CoinGecko, odświeżany co 5 minut
PriceAlert        // należy do ApplicationUser; Direction = Above | Below

### Kluczowe relacje

- ApplicationUser 1 ── * Wallet
- Wallet 1 ── * Holding
- Wallet 1 ── * Transaction
- ApplicationUser 1 ── * PriceAlert

### Ważna logika biznesowa

*Średnia ważona ceny zakupu* (aktualizowana przy każdej transakcji Buy):

nowaŚrednia = (staryQuantity × staraCena + nowyQuantity × nowaCena) / (staryQuantity + nowyQuantity)

*Zysk/Strata dla holdingu:*

PL = (aktualnaСena - averageBuyPrice) × quantity
PLPercent = ((aktualnaСena - averageBuyPrice) / averageBuyPrice) × 100

-----

## Konwencje nazewnictwa

### Backend (C#)

|Element                   |Konwencja           |Przykład                    |
|--------------------------|--------------------|----------------------------|
|Interfejs serwisu         |`I{Nazwa}Service`   |`IWalletService`            |
|Implementacja serwisu     |`{Nazwa}Service`    |`WalletService`             |
|Interfejs repozytorium    |`I{Nazwa}Repository`|`IWalletRepository`         |
|Implementacja repozytorium|`{Nazwa}Repository` |`WalletRepository`          |
|Kontroler                 |`{Nazwa}Controller` |`WalletController`          |
|DTO wejściowe             |`Create{Nazwa}Dto`  |`CreateWalletDto`           |
|DTO wyjściowe             |`{Nazwa}Dto`        |`WalletDto`                 |
|Metody async              |sufiks Async      |`GetAllAsync`, `CreateAsync`|

### Frontend (Angular / TypeScript)

|Element            |Konwencja               |Przykład                  |
|-------------------|------------------------|--------------------------|
|Komponent          |`{nazwa}.component.ts`  |`wallet-list.component.ts`|
|Serwis             |`{nazwa}.service.ts`    |`wallet.service.ts`       |
|Model/interfejs    |`{nazwa}.model.ts`      |`wallet.model.ts`         |
|Guard              |`{nazwa}.guard.ts`      |`auth.guard.ts`           |
|Interceptor        |`{nazwa}.interceptor.ts`|`jwt.interceptor.ts`      |
|Selector komponentu|`app-{nazwa}`           |`app-wallet-list`         |

-----

## Wzorce kodu — gotowe szablony

### Interfejs repozytorium

public interface I{Nazwa}Repository
{
    Task<IEnumerable<{Encja}>> GetAllByUserAsync(string userId);
    Task<{Encja}?> GetByIdAsync(int id);
    Task<{Encja}> AddAsync({Encja} entity);
    Task UpdateAsync({Encja} entity);
    Task DeleteAsync(int id);
}

### Implementacja repozytorium

public class {Nazwa}Repository : I{Nazwa}Repository
{
    private readonly AppDbContext _context;

    public {Nazwa}Repository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<{Encja}>> GetAllByUserAsync(string userId)
    {
        return await _context.{Tabela}
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }
}

### Kontroler (wzorzec standardowy)

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class {Nazwa}Controller : ControllerBase
{
    private readonly I{Nazwa}Service _service;

    public {Nazwa}Controller(I{Nazwa}Service service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync(UserId);
        return Ok(result);
    }
}

### Serwis Angular (wzorzec standardowy)

@Injectable({ providedIn: 'root' })
export class {Nazwa}Service {
  private readonly apiUrl = `${environment.apiUrl}/{endpoint}`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<{Model}[]> {
    return this.http.get<{Model}[]>(this.apiUrl);
  }

  create(dto: Create{Model}Dto): Observable<{Model}> {
    return this.http.post<{Model}>(this.apiUrl, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

-----

## Prompty dla typowych zadań

Kopiuj i wklejaj bezpośrednio do Copilot CLI, OpenCode lub Gemini CLI.

### Backend

*Nowe repozytorium:*

Wygeneruj interfejs I{Nazwa}Repository i jego implementację {Nazwa}Repository
dla encji {Encja} zgodnie ze wzorcem w AGENTS.md. Użyj AppDbContext.
Dodaj metodę GetAllByUserAsync filtrującą po UserId.

*Nowy serwis:*

Wygeneruj interfejs I{Nazwa}Service i implementację {Nazwa}Service.
Wstrzyknij I{Nazwa}Repository przez konstruktor.
Metody: GetAllAsync(userId), GetByIdAsync(id, userId), CreateAsync(dto, userId).
W GetByIdAsync sprawdź czy zasób należy do userId — jeśli nie, rzuć UnauthorizedAccessException.

*Nowy kontroler:*

Wygeneruj {Nazwa}Controller dziedziczący po ControllerBase.
Użyj atrybutu [Authorize]. UserId pobieraj z User.FindFirstValue(ClaimTypes.NameIdentifier).
Wstrzyknij I{Nazwa}Service. Zaimplementuj GET all, GET by id, POST, DELETE.

*Migracja EF Core po zmianie modelu:*

Dodaj migrację EF Core dla zmiany w klasie {Encja}: {opis zmiany}.
Podaj komendę dotnet ef i wyjaśnij co zmienia migracja.

*Background job CoinGecko:*

Wygeneruj klasę PriceBackgroundService dziedziczącą po BackgroundService.
W ExecuteAsync wywołuj IPriceService.RefreshCacheAsync() co 5 minut w pętli.
Użyj IServiceScopeFactory do resolve'owania IPriceService (jest scoped, nie singleton).
Obsłuż CancellationToken.

### Frontend Angular

*Nowy komponent standalone:*

Wygeneruj standalone Angular 17 komponent {Nazwa}Component.
Wstrzyknij {Nazwa}Service przez inject().
W ngOnInit pobierz dane i przypisz do zmiennej. Użyj AsyncPipe w template.
Obsłuż stan loading i błędy. Selector: app-{nazwa}.

*Nowy serwis Angular:*

Wygeneruj {Nazwa}Service dla Angular 17 zgodnie ze wzorcem w AGENTS.md.
BaseUrl: environment.apiUrl + '/{endpoint}'.
Dodaj metody: getAll(), getById(id), create(dto), delete(id).
Użyj HttpClient, zwracaj Observable.

*Formularz reaktywny:*

Wygeneruj formularz reaktywny Angular dla {Nazwa} z polami: {lista pól z typami}.
Użyj FormBuilder, dodaj walidatory Validators.required tam gdzie potrzeba.
Po submit wywołaj {Nazwa}Service.create() i obsłuż błąd z API.

*Guard autoryzacji:*

Wygeneruj AuthGuard jako funkcjonalny guard Angular 17 (inject-based, nie class-based).
Sprawdzaj obecność tokenu JWT w localStorage.
Jeśli brak — redirect na /login i return false. Jeśli jest — return true.

-----

## Czego NIE generować

Poniższe rzeczy są celowo pominięte lub mają specyficzne wymagania — nie generuj ich bez konsultacji z zespołem:

- *Nie generuj własnego hashowania haseł* — używamy ASP.NET Identity, który robi to automatycznie.
- *Nie dodawaj logiki do kontrolerów* — każda logika idzie do serwisu.
- **Nie używaj _context bezpośrednio w serwisach** — tylko przez interfejs repozytorium.
- *Nie generuj wielu implementacji jednego interfejsu* — jeden interfejs = jedna implementacja.
- **Nie używaj any w TypeScript** — zawsze typuj modele przez interfejsy w *.model.ts.
- *Nie generuj komponentów bez standalone: true* — projekt używa wyłącznie standalone components.
- *Nie wywołuj CoinGecko API bezpośrednio z kontrolera* — zawsze przez IPriceService.

-----

## Endpointy API (reference)

|Metoda|Endpoint                    |Opis                      |
|------|----------------------------|--------------------------|
|POST  |`/api/auth/register`        |Rejestracja               |
|POST  |`/api/auth/login`           |Logowanie → JWT           |
|GET   |`/api/wallets`              |Lista portfeli użytkownika|
|POST  |`/api/wallets`              |Nowy portfel              |
|DELETE|`/api/wallets/{id}`         |Usuń portfel              |
|GET   |`/api/wallets/{id}/holdings`|Aktywa portfela           |
|GET   |`/api/transactions`         |Historia transakcji       |
|POST  |`/api/transactions`         |Nowa transakcja (Buy/Sell)|
|GET   |`/api/portfolio/summary`    |Łączna wartość + P&L      |
|GET   |`/api/prices/{coinId}`      |Aktualna cena kryptowaluty|
|GET   |`/api/alerts`               |Lista alertów             |
|POST  |`/api/alerts`               |Nowy alert cenowy         |
|DELETE|`/api/alerts/{id}`          |Usuń alert                |

Wszystkie endpointy poza /api/auth/* wymagają nagłówka Authorization: Bearer <token>.

-----

## Wskazówki dla konkretnych narzędzi

### GitHub Copilot CLI

# Zapytaj o komendę którą chcesz wykonać
gh copilot suggest "dodaj migrację EF Core dla nowej kolumny w Wallet"

# Wyjaśnij komendę zanim ją uruchomisz
gh copilot explain "dotnet ef migrations add AddWalletDescription"

# Przekaż kontekst projektu
gh copilot suggest -t shell "uruchom tylko testy dla warstwy serwisów w investee-api"

### OpenCode

OpenCode automatycznie wczytuje ten plik (AGENTS.md) z roota projektu.
Nie musisz za każdym razem opisywać architektury — możesz od razu podać zadanie:

Zaimplementuj CreateAsync w TransactionService zgodnie z AGENTS.md.
Pamiętaj o aktualizacji Holding z użyciem średniej ważonej opisanej w sekcji "logika biznesowa".

### Gemini CLI

Gemini CLI jest najlepszy do analizy i planowania. Używaj go do:

# Przegląd kodu przed PR
gemini "Przejrzyj plik TransactionService.cs pod kątem zgodności z architekturą opisaną w AGENTS.md"

# Planowanie nowej funkcjonalności
gemini "Zaplanuj implementację eksportu historii transakcji do CSV w projekcie Investee zgodnie z AGENTS.md"

# Code review
gemini "Czy ten kod repozytorium jest zgodny z wzorcem z AGENTS.md? [wklej kod]"

-----