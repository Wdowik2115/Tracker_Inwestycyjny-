# Wallet Overview Module — Code Review

**Branch:** `wallet_overview`  
**Project:** Investe — ASP.NET Core + Angular cryptocurrency portfolio tracker  
**Author:** Jekon4ik  
**Date:** 2026-05-20

---

## 1. Overview

This branch introduces the **Wallet Overview** feature — a complete module that allows users to:

- Create and manage multiple cryptocurrency wallets
- See the current value and unrealized P&L of each wallet
- Drill into individual wallets to see per-asset breakdowns and a realized P&L calculation
- View a historical portfolio value chart (up to 1 year)
- See a global portfolio summary in the navigation header

The module spans all layers of the application: a new database table, a repository, application services, a REST controller, and Angular components with interactive charts.

---

## 2. Database Layer

### 2.1 New Model — `PriceHistoryCache`

**File:** [`Serwer/Serwer/Models/PriceHistoryCache.cs`](Serwer/Serwer/Models/PriceHistoryCache.cs)

```csharp
public class PriceHistoryCache
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CoinId { get; set; } = string.Empty;
    public DateTime Date { get; set; }      // UTC midnight — one row per coin per day
    public decimal PriceUsd { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
```

**Purpose:** Historical portfolio charts require the price of every held asset for every day in the past N days. Without caching, this would require hundreds of API calls to CoinGecko on every request. This table stores one row per `(CoinId, Date)` pair so prices are fetched from the network at most once per day.

- `CoinId` is the CoinGecko identifier (e.g. `"bitcoin"`, `"ethereum"`), not the ticker symbol.
- `Date` is always UTC midnight — this enforces one entry per calendar day.
- `FetchedAt` lets the service decide whether today's price is stale (older than 4 hours) and needs to be refreshed.

### 2.2 DbContext Update

**File:** [`Serwer/Serwer/Infrastructure/Persistence/ApplicationDbContext.cs`](Serwer/Serwer/Infrastructure/Persistence/ApplicationDbContext.cs)

Added to `ApplicationDbContext`:

```csharp
public DbSet<PriceHistoryCache> PriceHistoryCache => Set<PriceHistoryCache>();
```

The `OnModelCreating` configuration:

```csharp
modelBuilder.Entity<PriceHistoryCache>(e =>
{
    e.HasKey(p => p.Id);
    e.Property(p => p.Id).ValueGeneratedOnAdd();
    e.HasIndex(p => new { p.CoinId, p.Date }).IsUnique(); // prevents duplicate entries
    e.Property(p => p.PriceUsd).HasPrecision(18, 8);      // 8 decimal places for crypto prices
});
```

The **unique composite index** on `(CoinId, Date)` is the key constraint — it ensures there can never be two rows for the same coin on the same day, which prevents data duplication bugs.

### 2.3 Migration

**File:** [`Serwer/Serwer/Migrations/20260519221011_InitialCreate.cs`](Serwer/Serwer/Migrations/20260519221011_InitialCreate.cs)

Previous incremental migrations were consolidated into a single clean `InitialCreate` migration. It creates all tables:

| Table | Description |
|-------|-------------|
| `AspNetUsers` | Identity user accounts |
| `Wallets` | User-owned wallets |
| `Assets` | Holdings per wallet |
| `Transactions` | Buy/sell history |
| `PriceAlerts` | User-defined price alerts |
| `PriceHistoryCache` | **New** — daily price cache |

---

## 3. Repository / Data Access Layer

### 3.1 `IPriceHistoryCacheRepository`

**File:** [`Serwer/Serwer/Infrastructure/Persistence/Repositories/IPriceHistoryCacheRepository.cs`](Serwer/Serwer/Infrastructure/Persistence/Repositories/IPriceHistoryCacheRepository.cs)

```csharp
public interface IPriceHistoryCacheRepository : IBaseRepository<PriceHistoryCache>
{
    Task<List<PriceHistoryCache>> GetByCoinAndDateRangeAsync(string coinId, DateTime from, DateTime to);
}
```

Inherits CRUD operations from `IBaseRepository<T>` (existing pattern in the project) and adds one domain-specific method for querying a date range.

### 3.2 `IUnitOfWork` Update

**File:** [`Serwer/Serwer/Infrastructure/Persistence/UnitOfWork/IUnitOfWork.cs`](Serwer/Serwer/Infrastructure/Persistence/UnitOfWork/IUnitOfWork.cs)

```csharp
public interface IUnitOfWork : IDisposable
{
    IWalletRepository Wallets { get; }
    IAssetRepository Assets { get; }
    ITransactionRepository Transactions { get; }
    IPriceAlertRepository PriceAlerts { get; }
    IUserRepository Users { get; }
    IPriceHistoryCacheRepository PriceHistory { get; }   // ← new
    Task<int> CompleteAsync();
}
```

The Unit of Work pattern groups all repositories under a single `CompleteAsync()` call (a single database transaction). Adding `PriceHistory` here keeps the pattern consistent.

---

## 4. Application Layer — DTOs

DTOs (Data Transfer Objects) are the data contracts between the backend and frontend. They carry only the fields the client needs and contain no business logic.

**File location:** [`Serwer/Serwer/Application/DTOs/`](Serwer/Serwer/Application/DTOs/)

### `WalletDto`
```csharp
public class WalletDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal TotalValue { get; set; }   // Σ(quantity × currentPrice) for all assets
    public int AssetCount { get; set; }
    public decimal Pnl { get; set; }          // TotalValue − cost basis
    public decimal PnlPercent { get; set; }   // Pnl / costBasis × 100
}
```
Used for the wallet list (dashboard cards).

### `WalletDetailsDto`
Extends `WalletDto`:
```csharp
public class WalletDetailsDto : WalletDto
{
    public List<PositionDto> Assets { get; set; }
    public decimal RealizedPnl { get; set; }   // P&L from completed sales
}
```
Used for the wallet detail page.

### `PositionDto`
```csharp
public class PositionDto
{
    public string Symbol { get; set; }         // e.g. "BTC"
    public string Name { get; set; }           // e.g. "Bitcoin"
    public decimal Quantity { get; set; }      // amount currently held
    public decimal AvgCostBasis { get; set; }  // weighted average purchase price
    public decimal CurrentPrice { get; set; }  // live price from CoinGecko
    public decimal Value { get; set; }         // Quantity × CurrentPrice
    public decimal Pnl { get; set; }
    public decimal PnlPercent { get; set; }
}
```
One row in the assets table on the wallet detail page.

### `PortfolioSummaryDto`
```csharp
public class PortfolioSummaryDto
{
    public List<PositionDto> Positions { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalPnl { get; set; }
    public decimal TotalInvested { get; set; }
}
```
Aggregate across all wallets — used in the portfolio header.

### `WalletHistoryDto` + `HistoryPointDto`
```csharp
public class HistoryPointDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }   // portfolio value on that day
}

public class WalletHistoryDto
{
    public Guid WalletId { get; set; }
    public List<HistoryPointDto> Points { get; set; }
}
```
Time-series data powering the area charts.

---

## 5. Application Layer — Services

### 5.1 `ICoinPriceService` / `CoinPriceService`

**Files:**  
[`Application/Interfaces/Services/ICoinPriceService.cs`](Serwer/Serwer/Application/Interfaces/Services/ICoinPriceService.cs)  
[`Application/Services/CoinPriceService.cs`](Serwer/Serwer/Application/Services/CoinPriceService.cs)

This service is responsible for all communication with the CoinGecko API and for managing the price cache.

**Interface:**
```csharp
public interface ICoinPriceService
{
    Task<decimal> GetCurrentPriceAsync(string symbol);
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols);
    Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date);
    Task<List<HistoryPointDto>> GetPriceHistoryAsync(string symbol, int days);
}
```

**Symbol → CoinGecko ID mapping:**
```csharp
private static readonly Dictionary<string, string> SymbolToId = new(StringComparer.OrdinalIgnoreCase)
{
    ["BTC"] = "bitcoin", ["ETH"] = "ethereum", ["SOL"] = "solana",
    ["BNB"] = "binancecoin", ["USDT"] = "tether", ["USDC"] = "usd-coin",
    ["ADA"] = "cardano", ["DOT"] = "polkadot", ["AVAX"] = "avalanche-2",
    ["MATIC"] = "matic-network", ["LINK"] = "chainlink", ["XRP"] = "ripple"
};
```

**Two-tier caching strategy:**

| Layer | What | TTL |
|-------|------|-----|
| `IMemoryCache` | Current live prices | 30 seconds |
| `PriceHistoryCache` table | Historical daily prices | Permanent (re-fetched if today's entry > 4 h old) |

**`GetCurrentPricesAsync` — batch fetch:**  
Instead of making one API call per coin, this method collects all uncached symbols and fetches them in a **single** CoinGecko request:
```csharp
var ids = string.Join(",", uncachedIds.Select(x => x.coinId));
var response = await client.GetStringAsync($"simple/price?ids={ids}&vs_currencies=usd");
```

**`GetPriceHistoryAsync` — smart DB cache:**
1. Query existing rows from `PriceHistoryCache` for the requested date range.
2. Check if all past dates are present **and** today's entry was fetched recently.
3. If not — call CoinGecko `market_chart` endpoint and upsert the results into the DB.
4. Return the cached data as `List<HistoryPointDto>`.

### 5.2 `IWalletService` / `WalletService`

**Files:**  
[`Application/Interfaces/Services/IWalletService.cs`](Serwer/Serwer/Application/Interfaces/Services/IWalletService.cs)  
[`Application/Services/WalletService.cs`](Serwer/Serwer/Application/Services/WalletService.cs)

**Interface:**
```csharp
public interface IWalletService
{
    Task<WalletDto> CreateWalletAsync(Guid userId, CreateWalletDto dto);
    Task<IEnumerable<WalletDto>> GetUserWalletsAsync(Guid userId);
    Task<WalletDetailsDto> GetWalletDetailsAsync(Guid userId, Guid walletId);
    Task<WalletDto> UpdateWalletAsync(Guid userId, Guid walletId, UpdateWalletDto dto);
    Task DeleteWalletAsync(Guid userId, Guid walletId);
    Task<WalletHistoryDto> GetWalletHistoryAsync(Guid userId, Guid walletId, int days);
}
```

#### `GetUserWalletsAsync` — Batch pricing
All wallets' assets are collected first, then a **single** `GetCurrentPricesAsync` call prices everything at once. Without this, an N-wallet portfolio would make N separate API calls.

```csharp
var symbols = allAssets.Select(x => x.asset.Symbol).Distinct();
var prices = await _priceService.GetCurrentPricesAsync(symbols);  // 1 API call for all

return walletList.Select(wallet => {
    var totalValue = walletAssets.Sum(x => x.asset.Quantity * prices[x.asset.Symbol]);
    var costBasis  = walletAssets.Sum(x => x.asset.Quantity * x.asset.AverageBuyPrice);
    var pnl = totalValue - costBasis;
    // ...
});
```

#### `GetWalletDetailsAsync` — Unrealized and Realized P&L

**Unrealized P&L** — current market value vs. what was paid:
```csharp
var value = asset.Quantity * currentPrice;
var costBasis = asset.Quantity * asset.AverageBuyPrice;
var pnl = value - costBasis;
```

**Realized P&L** — calculated by the private `CalculateRealizedPnl` method using the **Weighted Average Cost (AVCO)** method:
```csharp
private static decimal CalculateRealizedPnl(IEnumerable<Transaction> transactions)
{
    decimal realized = 0;
    // group by symbol, replay chronologically
    foreach (var group in transactions.GroupBy(t => t.Symbol))
    {
        decimal avgCost = 0, qty = 0;
        foreach (var tx in group.OrderBy(t => t.ExecutedAt))
        {
            if (tx.Type == TransactionType.Buy)
            {
                // update weighted average on each buy
                var totalCost = qty * avgCost + tx.Quantity * tx.PriceAtTime;
                qty += tx.Quantity;
                avgCost = qty > 0 ? totalCost / qty : 0;
            }
            else // Sell
            {
                realized += (tx.PriceAtTime - avgCost) * tx.Quantity;
                qty = Math.Max(0, qty - tx.Quantity);
            }
        }
    }
    return realized;
}
```

#### `GetWalletHistoryAsync` — Transaction Replay

This method reconstructs what the portfolio was worth on each past day. It does not simply look at current assets — it **replays all transactions chronologically** to accurately reflect when assets were actually acquired or sold:

```csharp
foreach (var date in allDates)
{
    // Holdings = all buys minus all sells up to this date
    var holdings = new Dictionary<string, decimal>();
    foreach (var tx in allTxs.Where(t => t.ExecutedAt.Date <= date))
    {
        holdings[tx.Symbol] = tx.Type == Buy
            ? holdings[tx.Symbol] + tx.Quantity
            : holdings[tx.Symbol] - tx.Quantity;
    }
    // Portfolio value = Σ(holding[symbol] × price[symbol][date])
    var value = holdings.Sum(h => h.Value * priceHistory[h.Key][date]);
}
```

### 5.3 `PortfolioService`

**File:** [`Application/Services/PortfolioService.cs`](Serwer/Serwer/Application/Services/PortfolioService.cs)

Aggregates all wallets into a single portfolio-wide summary. It groups assets by symbol across all wallets and computes a **cross-wallet weighted average cost**:

```csharp
foreach (var group in allAssets.GroupBy(a => a.Symbol))
{
    var totalQty = group.Sum(a => a.Quantity);
    var weightedCost = group.Sum(a => a.Quantity * a.AvgCost);
    var avgCost = totalQty > 0 ? weightedCost / totalQty : 0m;
    // ...
}
```

---

## 6. Controller Layer

**File:** [`Serwer/Serwer/Controllers/WalletController.cs`](Serwer/Serwer/Controllers/WalletController.cs)

```csharp
[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    // constructor injection...
}
```

All endpoints require a valid JWT token (`[Authorize]`). The user's identity is extracted from the token claims via the existing `User.GetUserId()` extension method.

| Method | Route | Action | Returns |
|--------|-------|--------|---------|
| `GET` | `/api/wallets` | List wallets with P&L | `IEnumerable<WalletDto>` |
| `GET` | `/api/wallets/{id}` | Wallet details + positions | `WalletDetailsDto` |
| `POST` | `/api/wallets` | Create wallet | `201 Created` + `WalletDto` |
| `PUT` | `/api/wallets/{id}` | Update name/description | `WalletDto` |
| `DELETE` | `/api/wallets/{id}` | Delete wallet | `204 No Content` |
| `GET` | `/api/wallets/{id}/history?days=30` | Portfolio value history | `WalletHistoryDto` |

The `days` parameter on the history endpoint is clamped server-side to the range `[7, 365]`:
```csharp
var history = await _walletService.GetWalletHistoryAsync(
    User.GetUserId(), id, Math.Clamp(days, 7, 365));
```

---

## 7. Dependency Injection (`Program.cs`)

**File:** [`Serwer/Serwer/Program.cs`](Serwer/Serwer/Program.cs)

New service registrations added:

```csharp
// Named HTTP client for CoinGecko
builder.Services.AddHttpClient("CoinGecko", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    client.Timeout = TimeSpan.FromSeconds(10);
    // optional: client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
});

// Application services
builder.Services.AddScoped<ICoinPriceService, CoinPriceService>();
builder.Services.AddScoped<IWalletService, WalletService>();

// Repository
builder.Services.AddScoped<IPriceHistoryCacheRepository, PriceHistoryCacheRepository>();
```

All services are registered as **Scoped** — one instance per HTTP request, which is appropriate for services that use `DbContext`.

---

## 8. Frontend — TypeScript Models

**File:** [`Client/src/app/models/index.ts`](Client/src/app/models/index.ts)

New interfaces that mirror the backend DTOs exactly (Angular convention: camelCase):

```typescript
export interface WalletDto {
  id: string;
  name: string;
  description: string;
  totalValue: number;
  assetCount: number;
  pnl: number;
  pnlPercent: number;
}

export interface WalletDetailsDto extends WalletDto {
  assets: PositionDto[];
  realizedPnl: number;
}

export interface PositionDto {
  symbol: string; name: string; quantity: number;
  avgCostBasis: number; currentPrice: number;
  value: number; pnl: number; pnlPercent: number;
}

export interface PortfolioSummaryDto {
  positions: PositionDto[];
  totalValue: number; totalPnl: number; totalInvested: number;
}

export interface HistoryPoint {
  date: string;   // ISO date string
  value: number;
}

export interface WalletHistoryDto {
  walletId: string;
  points: HistoryPoint[];
}
```

---

## 9. Frontend — Angular Service

**File:** [`Client/src/app/services/wallet.service.ts`](Client/src/app/services/wallet.service.ts)

```typescript
@Injectable({ providedIn: 'root' })
export class WalletService {
  private apiUrl = `${environment.apiUrl}/wallets`;

  constructor(private http: HttpClient) {}

  getWallets(): Observable<WalletDto[]>
  getWallet(id: string): Observable<WalletDetailsDto>
  createWallet(dto: CreateWalletDto): Observable<WalletDto>
  updateWallet(id: string, dto: UpdateWalletDto): Observable<WalletDto>
  deleteWallet(id: string): Observable<void>
  getWalletHistory(id: string, days = 30): Observable<WalletHistoryDto>
}
```

Six methods — each maps directly to one backend endpoint. The service uses Angular's `HttpClient` and returns `Observable<T>`, allowing components to subscribe reactively.

---

## 10. Frontend — Angular Components

### 10.1 Dashboard (`DashboardComponent`)

**File:** [`Client/src/app/components/dashboard/dashboard.component.ts`](Client/src/app/components/dashboard/dashboard.component.ts)

Displays a grid of wallet cards, each with a sparkline chart.

**Data flow:**
```
merge(timer(0, 30000), transactionAdded$)
  → getWallets()
  → forkJoin(wallets.map(w => getWalletHistory(w.id, 30)))
  → builds WalletCardData[] with sparkline points
```

The `merge` with `transactionAdded$` means the dashboard **auto-refreshes** both on a 30-second timer and immediately whenever the user records a new transaction.

Sparkline color is determined at runtime:
```typescript
sparklineColor: w.pnl >= 0 ? '#26a17b' : '#e74c3c'  // green if profit, red if loss
```

### 10.2 Wallet Detail (`WalletDetailComponent`)

**File:** [`Client/src/app/components/wallets/wallet-detail/wallet-detail.component.ts`](Client/src/app/components/wallets/wallet-detail/wallet-detail.component.ts)

The most complex component — loaded via `/wallets/:id`. On init it fires three parallel requests:
```typescript
forkJoin({
  wallet: this.walletService.getWallet(id),
  history: this.walletService.getWalletHistory(id, this.selectedPeriod()),
  txs:    this.transactionService.getTransactions({ walletId: id, pageSize: 50 })
})
```

**Charts (ApexCharts):**

| Chart | Type | Data source |
|-------|------|-------------|
| Allocation | Donut | `wallet.assets.map(a => a.value)` |
| Performance | Area | `history.points` (date + value) |
| Dashboard sparklines | Area (sparkline) | 30-day history per wallet |

**Period selector** — the user can choose 1W / 1M / 3M / 6M / 1Y. Changing the period calls:
```typescript
changePeriod(days: number): void {
  this.selectedPeriod.set(days);
  this.walletService.getWalletHistory(this.walletId, days).subscribe(
    h => this.history.set(h.points)
  );
}
```

**Computed signals** (Angular 17+) keep derived state up to date automatically:
```typescript
totalInvested = computed(() => this.wallet()?.totalValue - this.wallet()?.pnl);
allocationSeries = computed(() => this.wallet()?.assets.map(a => a.value));
historySeries = computed(() => this.history().map(p => ({ x: new Date(p.date).getTime(), y: p.value })));
```

### 10.3 Portfolio Header (`PortfolioHeaderComponent`)

**File:** [`Client/src/app/components/layout/portfolio-header/portfolio-header.component.ts`](Client/src/app/components/layout/portfolio-header/portfolio-header.component.ts)

Displayed in the persistent navigation bar. Shows global portfolio totals across all wallets.

```typescript
this.refreshSub = merge(
  timer(0, 30000),
  this.transactionService.transactionAdded$
).pipe(
  switchMap(() => this.portfolioService.getSummary())
).subscribe(data => this.portfolio.set(data));
```

Features:
- **Auto-refresh** every 30 seconds + on any transaction event
- **Hide values toggle** — `hideValues` signal, masks numbers with `••••`
- **Multi-currency formatting** — reads user's `preferredCurrency` from profile, displays symbol (USD `$`, EUR `€`, GBP `£`, PLN `zł`)

---

## 11. Unit Tests

**File:** [`Serwer/Server.Tests/PortfolioServiceTests.cs`](Serwer/Server.Tests/PortfolioServiceTests.cs)

Two tests covering `PortfolioService.GetSummaryAsync`:

1. **`GetSummaryAsync_SingleAsset_ReturnsCorrectPosition`**  
   Given one wallet with 1 BTC bought at $50,000 and a current price of $60,000 — verifies that `TotalValue = 60,000`, `TotalPnl = 10,000`, and `TotalInvested = 50,000`.

2. **`GetSummaryAsync_EmptyPortfolio_ReturnsZeroTotals`**  
   Given a user with no wallets — verifies all totals are zero and the positions list is empty.

---

## 12. End-to-End Data Flow

```
┌──────────────────────────────────────────────────────────────────┐
│  Angular Frontend                                                │
│                                                                  │
│  PortfolioHeaderComponent                                        │
│    └── PortfolioService.getSummary()                             │
│          └── GET /api/portfolio/summary                          │
│                                                                  │
│  DashboardComponent                                              │
│    └── WalletService.getWallets()      → GET /api/wallets        │
│    └── WalletService.getWalletHistory()→ GET /api/wallets/{id}/history │
│                                                                  │
│  WalletDetailComponent                                           │
│    ├── WalletService.getWallet(id)     → GET /api/wallets/{id}   │
│    └── WalletService.getWalletHistory()→ GET /api/wallets/{id}/history │
└──────────────────────────────────────────────────────────────────┘
                          │  HTTP + JWT
                          ▼
┌──────────────────────────────────────────────────────────────────┐
│  ASP.NET Core Backend                                            │
│                                                                  │
│  WalletController   [Authorize]                                  │
│    ├── GetWallets()      → IWalletService.GetUserWalletsAsync()  │
│    ├── GetWalletDetails()→ IWalletService.GetWalletDetailsAsync()│
│    └── GetWalletHistory()→ IWalletService.GetWalletHistoryAsync()│
│                                                                  │
│  WalletService                                                   │
│    ├── IUnitOfWork.Wallets / Assets / Transactions               │
│    └── ICoinPriceService.GetCurrentPricesAsync()  (batch)        │
│                                                                  │
│  CoinPriceService                                                │
│    ├── IMemoryCache (current prices, 30s TTL)                    │
│    ├── IUnitOfWork.PriceHistory (DB cache for history)           │
│    └── HttpClient "CoinGecko" → api.coingecko.com               │
└──────────────────────────────────────────────────────────────────┘
                          │  EF Core
                          ▼
┌──────────────────────────────────────────────────────────────────┐
│  SQL Server Database                                             │
│                                                                  │
│  PriceHistoryCache  (CoinId, Date [unique], PriceUsd, FetchedAt)│
│  Wallets            (Id, UserId FK, Name, Description)          │
│  Assets             (Id, WalletId FK, Symbol, Quantity, AvgBuy) │
│  Transactions       (Id, WalletId FK, Type, Quantity, Price, …) │
└──────────────────────────────────────────────────────────────────┘
```

---

## 13. Summary of New / Modified Files

### Backend (`Serwer/`)

| File | Change |
|------|--------|
| `Models/PriceHistoryCache.cs` | **New** — entity model |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | Added `DbSet`, unique index, decimal precision |
| `Migrations/20260519221011_InitialCreate.cs` | Consolidated migration (replaces 3 previous) |
| `Infrastructure/Persistence/Repositories/IPriceHistoryCacheRepository.cs` | **New** — repository interface |
| `Infrastructure/Persistence/Repositories/Implementations/PriceHistoryCacheRepository.cs` | **New** — repository implementation |
| `Infrastructure/Persistence/UnitOfWork/IUnitOfWork.cs` | Added `PriceHistory` property |
| `Infrastructure/Persistence/UnitOfWork/UnitOfWork.cs` | Implemented `PriceHistory` property |
| `Application/DTOs/WalletDto.cs` | **New** |
| `Application/DTOs/WalletDetailsDto.cs` | **New** |
| `Application/DTOs/PositionDto.cs` | **New** |
| `Application/DTOs/PortfolioSummaryDto.cs` | **New** |
| `Application/DTOs/WalletHistoryDto.cs` | **New** |
| `Application/Interfaces/Services/ICoinPriceService.cs` | **New** |
| `Application/Interfaces/Services/IWalletService.cs` | **New** |
| `Application/Services/CoinPriceService.cs` | **New** |
| `Application/Services/WalletService.cs` | **New** |
| `Application/Services/PortfolioService.cs` | **New** |
| `Controllers/WalletController.cs` | **New** |
| `Program.cs` | DI registrations + HttpClient config |

### Frontend (`Client/src/`)

| File | Change |
|------|--------|
| `app/models/index.ts` | Added wallet, portfolio, history interfaces |
| `app/services/wallet.service.ts` | **New** — Angular wallet service |
| `app/components/dashboard/dashboard.component.ts` | Rewritten — sparkline charts, auto-refresh |
| `app/components/wallets/wallet-detail/wallet-detail.component.ts` | **New** — full detail view |
| `app/components/layout/portfolio-header/portfolio-header.component.ts` | **New** — global portfolio bar |
| `app/components/layout/shared-layout.component.ts` | Added portfolio header |

### Tests

| File | Change |
|------|--------|
| `Server.Tests/PortfolioServiceTests.cs` | **New** — 2 unit tests |
