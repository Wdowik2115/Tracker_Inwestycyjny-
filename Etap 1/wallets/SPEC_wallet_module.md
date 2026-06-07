# Wallet Module — Sequential UML Specification

## Overview

This document specifies all sequential flows in the **Wallet Module** of the crypto portfolio tracker (ASP.NET 8 backend + Angular 17 frontend). Each flow is documented as a PlantUML `.puml` file in this folder.

---

## Architecture Layers

| Layer | Technology | Role |
|---|---|---|
| Frontend | Angular 17 | Components, Services, HTTP Client with JWT interceptor |
| API | ASP.NET 8 Web API | REST controllers, model validation |
| Middleware | `ErrorHandlingMiddleware` | Global exception → HTTP status mapping |
| Application | `WalletService`, `TransactionService` | Business logic, P&L calculations |
| Infrastructure | `IUnitOfWork`, Repositories | EF Core data access |
| External | CoinGecko API | Real-time & historical crypto prices |
| Database | SQL Server (EF Core) | Persistent storage |

---

## Actors & Participants

| ID | Name | Description |
|---|---|---|
| `User` | User (Browser) | Authenticated user interacting via Angular SPA |
| `WalletsComp` | WalletsComponent | Angular wallet list/grid page |
| `DetailComp` | WalletDetailComponent | Angular wallet detail page (charts, positions, recent txns) |
| `TxnsComp` | TransactionsComponent | Angular transactions page (paginated table, filters) |
| `WalletSvc_FE` | WalletService (Angular) | Frontend HTTP wrapper for wallet endpoints |
| `TxnSvc_FE` | TransactionService (Angular) | Frontend HTTP wrapper for transaction endpoints |
| `HTTP` | HTTP Client + JWT Interceptor | Adds `Authorization: Bearer <token>` header |
| `MW` | ErrorHandlingMiddleware | Catches exceptions, returns structured error responses |
| `WC` | WalletController | `[Authorize]` API controller at `api/wallets` |
| `TC` | TransactionController | `[Authorize]` API controller at `api/transactions` |
| `WS` | WalletService (Backend) | `IWalletService` implementation |
| `TS` | TransactionService (Backend) | `ITransactionService` implementation |
| `UOW` | IUnitOfWork | Coordinates repositories, wraps `SaveChangesAsync()` |
| `WR` | WalletRepository | EF Core wallet data access |
| `TR` | TransactionRepository | EF Core transaction data access |
| `AR` | AssetRepository | EF Core asset (current holdings) data access |
| `CPS` | ICoinPriceService | Fetches prices from CoinGecko (with cache layer) |
| `DB` | Database (SQL Server) | Persistent storage via EF Core |
| `CG` | CoinGecko API | External crypto price & history provider |

---

## Domain Models

### Wallet
```
Id (Guid PK), Name, Description, UserId (FK), CreatedAt
→ ICollection<Asset> (cascade delete)
→ ICollection<Transaction> (cascade delete)
```

### Asset (Current Holdings)
```
Id (Guid PK), CoinId, Symbol, Name, Quantity (18,8), AverageBuyPrice (18,8), WalletId (FK)
```

### Transaction
```
Id (Guid PK), CoinId, Symbol, Type (Buy|Sell),
Quantity (18,8), PriceAtTime (18,8), TotalValue (18,8),
Fee (18,8), FeeCurrency, CostBasisPerUnit (18,8?), CostBasisSource,
ExecutedAt, Notes, WalletId (FK)
```

---

## Flows Documented

| File | Flow | HTTP Method & Route |
|---|---|---|
| `SEQ_01_get_wallets.puml` | Load wallet list with current values & P&L | `GET /api/wallets` |
| `SEQ_02_get_wallet_detail.puml` | Load wallet detail page (positions + charts) | `GET /api/wallets/{id}` |
| `SEQ_03_get_wallet_history.puml` | Load portfolio value history for charting | `GET /api/wallets/{id}/history?days=30` |
| `SEQ_04_create_wallet.puml` | Create a new wallet | `POST /api/wallets` |
| `SEQ_05_update_wallet.puml` | Update wallet name / description | `PUT /api/wallets/{id}` |
| `SEQ_06_delete_wallet.puml` | Delete wallet with cascade | `DELETE /api/wallets/{id}` |
| `SEQ_07_get_transactions.puml` | Get paginated, filtered transaction list | `GET /api/transactions?...` |
| `SEQ_08_add_transaction_buy.puml` | Add a BUY transaction (updates Asset) | `POST /api/transactions` (type=Buy) |
| `SEQ_09_add_transaction_sell.puml` | Add a SELL transaction (with quantity guard) | `POST /api/transactions` (type=Sell) |
| `SEQ_10_update_transaction.puml` | Update an existing transaction | `PUT /api/transactions/{id}` |
| `SEQ_11_delete_transaction.puml` | Delete transaction and reverse asset effect | `DELETE /api/transactions/{id}` |

---

## Key Business Rules

1. **Ownership verification** — every service method checks `wallet.UserId == authenticatedUserId` before any mutation; throws `UnauthorizedAccessException` on mismatch.
2. **Weighted average cost basis** — on BUY, new `AverageBuyPrice = (oldQty * oldAvg + newQty * newPrice) / (oldQty + newQty)`.
3. **Insufficient quantity guard** — SELL fails with `InvalidOperationException` if `asset.Quantity < sell.Quantity`.
4. **Auto cost basis** — if `TransactionCreateDto.CostBasisPerUnit` is null, the service fetches the historical price from `ICoinPriceService` and sets `CostBasisSource = "historical_price"`.
5. **Cascade deletes** — deleting a wallet removes all its Assets and Transactions via FK cascade in the database.
6. **Transaction reversal** — deleting or updating a transaction first reverses the original asset effect, then applies the new values.
7. **P&L types**:
   - *Unrealized P&L* = `(currentPrice - avgBuyPrice) * quantity` per asset.
   - *Realized P&L* = sum of `(sellPrice - costBasis) * sellQty` for all SELL transactions.
8. **Portfolio history** — replays all transactions day-by-day; applies historical daily prices to reconstructed holdings to produce a value time series.

---

## Authentication Flow

```
User logs in → JWT issued (HS256, issuer/audience validated)
↓
Angular HTTP Interceptor attaches: Authorization: Bearer <token>
↓
[Authorize] attribute on controller → 401 if missing/invalid
↓
Controller: userId = User.GetUserId()  ← reads NameIdentifier or "sub" claim
↓
Service: verifies ownership on every resource access
```

---

## Error Handling

`ErrorHandlingMiddleware` maps exceptions to HTTP responses:

| Exception | HTTP Status |
|---|---|
| `UnauthorizedAccessException` | `403 Forbidden` |
| `KeyNotFoundException` | `404 Not Found` |
| `InvalidOperationException` | `400 Bad Request` |
| Any other | `500 Internal Server Error` |
