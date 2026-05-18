# Price Alerts Feature Specification

**Project:** Investe - Cryptocurrency Investment Tracker  
**Feature:** Price Alerts (CRUD & Monitoring)  
**Version:** 1.0  
**Status:** Active Development  
**Last Updated:** 2026-05-18

---

## 1. Executive Summary

The **Price Alerts** feature enables users to set price targets for cryptocurrencies and receive notifications when those targets are reached. Users can create multiple alerts with customizable conditions (price above/below target) and monitor their activation status.

### Key Capabilities
- ✅ Create price alerts with target price and direction (Above/Below)
- ✅ View all personal alerts (active and triggered)
- ✅ Delete alerts
- ✅ Automatic price monitoring and alert triggering
- ✅ Authorization (users can only manage their own alerts)

---

## 2. Business Requirements

### 2.1 Functional Requirements

#### FR-1: Create Price Alert
- User can create a new price alert with:
  - **Symbol**: Cryptocurrency symbol (e.g., BTC, ETH) — required, max 20 chars
  - **Target Price**: Decimal value in USDT — required, > 0
  - **Direction**: "Above" or "Below" — required
- Alert is created in **active** state (not triggered)
- Each alert belongs to exactly one user
- System assigns unique identifier (GUID)
- Created alert is immediately stored in database

#### FR-2: Retrieve Price Alerts
- **List all alerts**: User retrieves all their alerts (active and triggered)
- **Get specific alert**: User retrieves details of one alert by ID
- Results include: Id, Symbol, TargetPrice, Direction, IsTriggered, TriggeredAt, CreatedAt
- Users can only retrieve their own alerts (authorization enforced)

#### FR-3: Update Price Alert
- User can modify alert before it is triggered:
  - Update **TargetPrice**
  - Update **Direction** (Above ↔ Below)
- Alert symbol cannot be changed (immutable)
- Update fails if alert is already triggered
- Changes are immediately persisted

#### FR-4: Delete Price Alert
- User can delete any of their alerts (active or triggered)
- Deletion is permanent and irreversible
- System removes alert from database

#### FR-5: Automatic Price Monitoring
- Background service checks all active alerts periodically (~5 min intervals)
- For each active alert, system fetches current price from CoinGecko API
- Compares current price with target price using alert direction:
  - **Direction = Above**: Trigger if `currentPrice >= targetPrice`
  - **Direction = Below**: Trigger if `currentPrice <= targetPrice`
- Triggered alerts are marked with:
  - `IsTriggered = true`
  - `TriggeredAt = DateTime.UtcNow` (timestamp of triggering)
- Triggered alerts are excluded from future price checks

#### FR-6: Alert State Management
- **Active State**: `IsTriggered = false`
  - Alert is monitored by background service
  - Can be updated or deleted
  - Counted in user's "Active" alert list
  
- **Triggered State**: `IsTriggered = true`
  - No longer monitored by background service
  - Can only be deleted (no updates)
  - Counted in user's "Triggered" alert list
  - Includes `TriggeredAt` timestamp

### 2.2 Non-Functional Requirements

#### NFR-1: Performance
- Alert creation: < 500ms
- Listing alerts: < 1s (even with 1000+ alerts)
- Price checking: Batch process all symbols to minimize API calls

#### NFR-2: Reliability
- Failed price checks do not crash background service
- Missing price data is gracefully skipped (no trigger)
- Database consistency maintained if create/update fails

#### NFR-3: Security
- All endpoints require JWT authentication
- Users can only access/modify their own alerts
- Alert query filtered by `UserId` at repository level
- No SQL injection vulnerabilities

#### NFR-4: Scalability
- Support hundreds of alerts per user
- Support millions of alerts system-wide
- Batch price checking to reduce API calls

---

## 3. Data Model

### 3.1 Domain Entity: PriceAlert

```csharp
public class PriceAlert
{
    public Guid Id { get; set; }                    // Unique identifier
    public Guid UserId { get; set; }                // Owner of alert
    public virtual User User { get; set; }          // Navigation: belongs to User
    public string CoinId { get; set; }              // Lowercase symbol (e.g., "bitcoin")
    public string Symbol { get; set; }              // Display symbol (e.g., "BTC")
    public decimal TargetPrice { get; set; }        // Price target in USDT
    public AlertDirection Direction { get; set; }   // Above or Below
    public bool IsTriggered { get; set; }           // Status flag
    public DateTime? TriggeredAt { get; set; }      // When alert triggered (nullable)
    public DateTime CreatedAt { get; set; }         // Creation timestamp (UTC)
}
```

### 3.2 Enum: AlertDirection

```csharp
public enum AlertDirection
{
    Above = 0,   // Trigger when price >= TargetPrice
    Below = 1    // Trigger when price <= TargetPrice
}
```

### 3.3 Relationships

```
ApplicationUser 1 ─── * PriceAlert
    (UserId)      (owned by)
```

- **Multiplicity**: One user can have many alerts
- **Cascade Delete**: When user is deleted, all their alerts are deleted
- **Foreign Key**: `PriceAlert.UserId` → `ApplicationUser.Id`

---

## 4. API Specification

### Base URL
```
/api/alerts
```

### 4.1 Create Alert
```
POST /api/alerts
Authorization: Bearer {jwt_token}
Content-Type: application/json

Request Body:
{
  "symbol": "BTC",
  "targetPrice": 50000.00,
  "direction": 0  // 0 = Above, 1 = Below
}

Response (201 Created):
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "symbol": "BTC",
  "targetPrice": 50000.00,
  "direction": 0,
  "isTriggered": false,
  "triggeredAt": null,
  "createdAt": "2026-05-18T21:04:57Z"
}

Error Responses:
- 400 Bad Request: Invalid input (symbol too long, targetPrice < 0, etc.)
- 401 Unauthorized: Missing or invalid JWT token
- 500 Internal Server Error: Database error
```

### 4.2 List All Alerts
```
GET /api/alerts
Authorization: Bearer {jwt_token}

Response (200 OK):
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "symbol": "BTC",
    "targetPrice": 50000.00,
    "direction": 0,
    "isTriggered": false,
    "triggeredAt": null,
    "createdAt": "2026-05-18T21:04:57Z"
  },
  ...
]

Error Responses:
- 401 Unauthorized: Missing or invalid JWT token
```

### 4.3 Get Alert by ID
```
GET /api/alerts/{id:guid}
Authorization: Bearer {jwt_token}

Response (200 OK):
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "symbol": "BTC",
  "targetPrice": 50000.00,
  "direction": 0,
  "isTriggered": false,
  "triggeredAt": null,
  "createdAt": "2026-05-18T21:04:57Z"
}

Error Responses:
- 401 Unauthorized: Missing or invalid JWT token
- 403 Forbidden: Alert belongs to different user
- 404 Not Found: Alert does not exist
```

### 4.4 Update Alert
```
PUT /api/alerts/{id:guid}
Authorization: Bearer {jwt_token}
Content-Type: application/json

Request Body:
{
  "targetPrice": 55000.00,
  "direction": 1  // Optional: can change direction
}

Response (200 OK):
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "symbol": "BTC",
  "targetPrice": 55000.00,
  "direction": 1,
  "isTriggered": false,
  "triggeredAt": null,
  "createdAt": "2026-05-18T21:04:57Z"
}

Error Responses:
- 400 Bad Request: targetPrice <= 0 or alert already triggered
- 401 Unauthorized: Missing or invalid JWT token
- 403 Forbidden: Alert belongs to different user
- 404 Not Found: Alert does not exist
- 409 Conflict: Cannot update triggered alert
```

### 4.5 Delete Alert
```
DELETE /api/alerts/{id:guid}
Authorization: Bearer {jwt_token}

Response (204 No Content):
(empty body)

Error Responses:
- 401 Unauthorized: Missing or invalid JWT token
- 403 Forbidden: Alert belongs to different user
- 404 Not Found: Alert does not exist
```

---

## 5. Architecture

### 5.1 Layered Architecture

```
┌─────────────────────────────────────────────┐
│  Frontend (Angular 17)                      │
│  - AlertsComponent                          │
│  - AlertService                             │
│  - Models: AlertDto, CreateAlertDto         │
└──────────────┬──────────────────────────────┘
               │ HTTP (REST)
┌──────────────▼──────────────────────────────┐
│  Controllers Layer                          │
│  - PriceAlertController                     │
│  - Routes: GET, POST, PUT, DELETE           │
└──────────────┬──────────────────────────────┘
               │ DI
┌──────────────▼──────────────────────────────┐
│  Service Layer (Business Logic)             │
│  - IPriceAlertService (interface)           │
│  - PriceAlertService (implementation)       │
│  - Methods: CRUD + CheckAndTriggerAsync     │
└──────────────┬──────────────────────────────┘
               │ DI
┌──────────────▼──────────────────────────────┐
│  Repository Layer (Data Access)             │
│  - IPriceAlertRepository (interface)        │
│  - PriceAlertRepository (implementation)    │
│  - UnitOfWork pattern                       │
└──────────────┬──────────────────────────────┘
               │ EF Core
┌──────────────▼──────────────────────────────┐
│  Database (MS SQL Server)                   │
│  - PriceAlerts table                        │
│  - Foreign key: UserId → AspNetUsers        │
└─────────────────────────────────────────────┘
```

### 5.2 Service Layer Responsibilities

**IPriceAlertService:**
- ✅ `CreateAlertAsync(userId, dto)` — Create new alert
- ✅ `GetUserAlertsAsync(userId)` — List all user alerts
- ❌ `GetAlertByIdAsync(id, userId)` — Get single alert (TODO)
- ❌ `UpdateAlertAsync(userId, id, dto)` — Update alert (TODO)
- ✅ `DeleteAlertAsync(userId, id)` — Delete alert
- ✅ `CheckAndTriggerAlertsAsync()` — Monitor & trigger alerts

### 5.3 Repository Layer Responsibilities

**IPriceAlertRepository:**
- ✅ `GetAlertsByUserIdAsync(userId)` — Retrieve user's alerts
- ✅ `GetActiveAlertsAsync()` — Retrieve all non-triggered alerts
- ✅ `GetByIdAsync(id)` — Get alert by ID
- ✅ `AddAsync(entity)` — Add new alert
- ❌ `UpdateAsync(entity)` — Update alert (TODO)
- ✅ `DeleteAsync(id)` — Delete alert

---

## 6. Business Logic Rules

### 6.1 Alert Creation
1. Symbol is converted to **uppercase** for display
2. CoinId is converted to **lowercase** for API calls
3. Alert starts in **active state** (`IsTriggered = false`)
4. `TriggeredAt` is **null** until alert triggers
5. `CreatedAt` is set to **UTC now**

### 6.2 Alert Triggering
1. Background service runs every **~5 minutes**
2. Only **active alerts** (IsTriggered = false) are checked
3. For each active alert:
   - Fetch current price from CoinGecko API
   - Compare with `TargetPrice` using `Direction`:
     - **Above**: `currentPrice >= targetPrice` → trigger
     - **Below**: `currentPrice <= targetPrice` → trigger
4. When triggered:
   - Set `IsTriggered = true`
   - Set `TriggeredAt = DateTime.UtcNow`
   - Log event (informational)
   - Persist to database
5. Failed price fetches are silently skipped (error logged)

### 6.3 Authorization Rules
1. Users can only **read** alerts they own
2. Users can only **update** alerts they own
3. Users can only **delete** alerts they own
4. Repository queries always filter by `UserId`
5. Service methods throw `UnauthorizedAccessException` if user ≠ owner

### 6.4 Update Constraints
1. **Updatable fields**: TargetPrice, Direction
2. **Immutable fields**: Symbol, CoinId (create new alert if change needed)
3. **Triggered alerts cannot be updated** (read-only after triggering)
4. TargetPrice must be > 0

### 6.5 Cleanup & Housekeeping
1. Triggered alerts remain in database indefinitely
2. Users can delete triggered alerts manually
3. No automatic pruning or archival (by design)

---

## 7. Data Flow Diagrams

### 7.1 Create Alert Flow
```
User (Frontend)
    ↓
[Fill form: Symbol, TargetPrice, Direction]
    ↓
POST /api/alerts
    ↓
PriceAlertController.CreateAlert()
    ↓
PriceAlertService.CreateAlertAsync()
    ├─ Validate input (ACTIVE MODEL: CreateAlertDto)
    ├─ Create PriceAlert entity
    ├─ Set default values (IsTriggered=false, CreatedAt=now)
    ↓
IPriceAlertRepository.AddAsync()
    ↓
EntityFramework (DbContext)
    ↓
MS SQL Server (INSERT)
    ↓
Return AlertDto (201 Created)
    ↓
Frontend receives alert → Display in list
```

### 7.2 Check & Trigger Alert Flow
```
[Background Service Timer - every ~5 min]
    ↓
PriceAlertService.CheckAndTriggerAlertsAsync()
    ↓
Get all active alerts (IsTriggered=false)
    ↓
For each distinct Symbol:
    ├─ Fetch price from CoinGeckoClient
    └─ Store in Dictionary<Symbol, Price>
    ↓
For each active alert:
    ├─ Get current price from dictionary
    ├─ Evaluate: (Direction=Above && price>=target) OR (Direction=Below && price<=target)
    ├─ If true:
    │  ├─ Set IsTriggered=true
    │  ├─ Set TriggeredAt=now
    │  ├─ Update in repository
    │  └─ Log event
    └─ If false: skip
    ↓
UnitOfWork.CompleteAsync() (batch update)
    ↓
MS SQL Server (UPDATE)
```

---

## 8. Error Handling

### 8.1 Backend Error Cases

| Scenario | HTTP Status | Response |
|----------|-------------|----------|
| Invalid symbol format | 400 | `{ "message": "Symbol must be 1-20 chars" }` |
| TargetPrice < 0 | 400 | `{ "message": "TargetPrice must be > 0" }` |
| Missing JWT token | 401 | `{ "message": "Authorization required" }` |
| Alert not found | 404 | `{ "message": "Alert not found" }` |
| User doesn't own alert | 403 | `{ "message": "Unauthorized" }` |
| Update triggered alert | 409 | `{ "message": "Cannot update triggered alert" }` |
| Database error | 500 | `{ "message": "Internal server error" }` |

### 8.2 Frontend Error Handling
- Display toast notifications for all errors
- Show loading spinners during API calls
- Disable submit buttons while submitting
- Validate form before submission (client-side)
- Handle 401 → redirect to /login
- Handle 403 → show "you don't own this alert"
- Handle 404 → reload list
- Handle 500 → show generic error message

---

## 9. Testing Strategy

### 9.1 Unit Tests (C#)
**PriceAlertService Tests:**
- ✅ CreateAlertAsync: valid input → returns AlertDto
- ❌ CreateAlertAsync: invalid symbol → throws ArgumentException (TODO)
- ✅ GetUserAlertsAsync: returns all user's alerts (TODO)
- ✅ DeleteAlertAsync: removes alert from db (TODO)
- ✅ DeleteAlertAsync: wrong user → throws UnauthorizedAccessException (TODO)
- ✅ CheckAndTriggerAlertsAsync: Above direction triggers correctly (TODO)
- ✅ CheckAndTriggerAlertsAsync: Below direction triggers correctly (TODO)
- ✅ CheckAndTriggerAlertsAsync: price edge cases (equal, just above, just below) (TODO)

### 9.2 Integration Tests (Controller)
- ✅ POST /api/alerts with valid dto → 201 Created
- ✅ GET /api/alerts → 200 OK with list
- ❌ GET /api/alerts/{id} → 200 OK with alert detail (TODO)
- ❌ PUT /api/alerts/{id} → 200 OK with updated alert (TODO)
- ✅ DELETE /api/alerts/{id} → 204 No Content
- ✅ PUT triggered alert → 409 Conflict (TODO)
- ✅ GET other user's alert → 403 Forbidden (TODO)

### 9.3 Frontend Tests (Angular)
- Test AlertsComponent loads alerts
- Test create alert form validation
- Test delete confirmation & API call
- Test error toast notifications
- Test active/triggered alert separation

---

## 10. Database Schema

### PriceAlerts Table
```sql
CREATE TABLE [dbo].[PriceAlerts] (
    [Id]           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT (NEWID()),
    [UserId]       UNIQUEIDENTIFIER NOT NULL,
    [CoinId]       NVARCHAR(50)     NOT NULL,       -- lowercase: "bitcoin", "ethereum"
    [Symbol]       NVARCHAR(20)     NOT NULL,       -- uppercase: "BTC", "ETH"
    [TargetPrice]  DECIMAL(18,8)    NOT NULL,       -- Price in USDT
    [Direction]    INT              NOT NULL,       -- 0=Above, 1=Below
    [IsTriggered]  BIT              NOT NULL DEFAULT 0,
    [TriggeredAt]  DATETIME2        NULL,           -- UTC timestamp
    [CreatedAt]    DATETIME2        NOT NULL DEFAULT (GETUTCDATE()),
    
    CONSTRAINT FK_PriceAlerts_User 
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

CREATE INDEX IX_PriceAlerts_UserId ON [dbo].[PriceAlerts]([UserId]);
CREATE INDEX IX_PriceAlerts_IsTriggered ON [dbo].[PriceAlerts]([IsTriggered]);
```

---

## 11. Deployment Checklist

- [ ] Run EF Core migrations (add PriceAlerts table if missing)
- [ ] Configure background service in Program.cs
- [ ] Set up CoinGecko API integration (IPriceService)
- [ ] Add authentication middleware (JWT validation)
- [ ] Deploy API endpoints
- [ ] Deploy frontend components
- [ ] Test all CRUD operations in staging
- [ ] Monitor alert triggering in production
- [ ] Set up logging/alerting for failures

---

## 12. Future Enhancements

1. **Notifications**: Email/SMS/Push when alert triggers
2. **Multiple Targets**: Alert when price crosses any of multiple targets
3. **Recurring Alerts**: Auto-recreate triggered alerts
4. **Alert History**: Soft-delete alerts, view archived alerts
5. **Batch Operations**: Create/delete multiple alerts at once
6. **Alert Analytics**: Charts of how many times user's alerts triggered
7. **Webhooks**: Call external service when alert triggers
8. **Alert Templates**: Saved alert configurations for quick reuse

---

## Glossary

| Term | Definition |
|------|-----------|
| **Alert** | A user-defined trigger that monitors a cryptocurrency price |
| **Active Alert** | Alert that has not yet triggered (`IsTriggered = false`) |
| **Triggered Alert** | Alert that has reached its price target (`IsTriggered = true`) |
| **Direction** | The condition for triggering: "Above" (>=) or "Below" (<=) |
| **TargetPrice** | The price threshold in USDT that activates the alert |
| **Symbol** | The display name of cryptocurrency (e.g., BTC, ETH) |
| **CoinId** | The lowercase identifier used in CoinGecko API (e.g., bitcoin, ethereum) |
| **Background Service** | Long-running process that monitors prices every ~5 minutes |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-05-18 | Implementation Team | Initial specification |

