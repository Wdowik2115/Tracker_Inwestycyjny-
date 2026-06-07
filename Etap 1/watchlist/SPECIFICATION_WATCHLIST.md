# Watchlist Feature Specification

**Project:** Investe - Cryptocurrency Investment Tracker  
**Feature:** Watchlist (Coins Monitoring)  
**Version:** 1.0  
**Status:** Implemented  
**Last Updated:** 2026-06-06

---

## 1. Executive Summary

The **Watchlist** feature allows users to keep track of cryptocurrencies they are interested in without necessarily owning them. It provides a centralized view of real-time prices for a personalized list of assets, bridging the gap between portfolio tracking and market monitoring.

### Key Capabilities
- ✅ Add coins to a personal watchlist by symbol
- ✅ Real-time price tracking via CoinGecko API
- ✅ Intelligent price formatting (2 decimals for >$1, up to 8 for "penny" coins)
- ✅ View all watched assets in a dedicated dashboard
- ✅ Remove coins from the watchlist
- ✅ Duplicate prevention (adding the same coin twice is handled gracefully)

---

## 2. Business Requirements

### 2.1 Functional Requirements

#### FR-1: Add Coin to Watchlist
- User can add a coin by entering its **Symbol** (e.g., BTC, DOGE).
- System automatically maps the symbol to a **CoinId** (CoinGecko identifier).
- If the coin is already on the user's watchlist, the system prevents duplicate entries.
- The asset is immediately stored in the database with a timestamp of when it was added.

#### FR-2: Retrieve Watchlist
- User can view a list of all watched assets.
- For each asset, the system displays:
  - **Symbol**: (e.g., BTC)
  - **CoinId**: (e.g., bitcoin)
  - **Current Price**: Fetched in real-time (USD)
  - **Date Added**: When the user started watching the coin
- Prices are automatically formatted for readability.

#### FR-3: Remove from Watchlist
- User can remove any asset from their watchlist.
- Removal is permanent but can be re-added later.
- System removes the record from the database.

#### FR-4: Real-time Price Integration
- System fetches the latest prices from CoinGecko API upon loading the watchlist.
- Batch requests are used to minimize API calls (all symbols in one request).
- 30-second server-side caching is applied to stay within API rate limits.

---

## 3. Data Model

### 3.1 Domain Entity: WatchlistItem

```csharp
public class WatchlistItem
{
    public Guid Id { get; set; }                    // Unique identifier
    public Guid UserId { get; set; }                // Owner of the entry
    public virtual User User { get; set; }          // Navigation: belongs to User
    public string CoinId { get; set; }              // Lowercase ID (e.g., "bitcoin")
    public string Symbol { get; set; }              // Display symbol (e.g., "BTC")
    public DateTime AddedAt { get; set; }           // Timestamp (UTC)
}
```

### 3.2 Relationships
- **ApplicationUser 1 ─── * WatchlistItem**: A user can have many coins on their watchlist.
- **Cascade Delete**: Deleting a user removes all their watchlist items.

---

## 4. API Specification

### Base URL: `/api/watchlist`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/watchlist` | Get all watched coins with current prices |
| POST | `/api/watchlist` | Add a new coin (needs `CoinId`, `Symbol`) |
| DELETE | `/api/watchlist/{id}` | Remove a coin from the list |
| GET | `/api/watchlist/check/{coinId}` | Check if a coin is already watched |

---

## 5. UI/UX Design
- **Theme**: Dark mode (consistent with Investe design system).
- **Layout**: 
    - Header with "Add to Watchlist" button.
    - Filter bar for quick symbol entry.
    - Data table with hover effects.
- **Feedback**: 
    - Loading spinners during API fetch.
    - Toast notifications for success/error.
    - Hover states for table rows.

---

## 7. Testing Coverage
- **Unit Tests**: `WatchlistServiceTests.cs` (7 test cases covering CRUD and edge cases).
- **Integration**: Verified communication between Angular Service and .NET Controller.
- **Manual**: Tested adding popular coins (BTC, ETH) and "penny" coins (DOGE, SHIB) for formatting.
