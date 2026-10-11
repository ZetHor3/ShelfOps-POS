# Architecture

## Overview

ShelfOps follows the **Modular Monolith** architectural pattern — a single deployable unit with clear separation of concerns across layers. This combines the development and deployment simplicity of a monolith with the maintainability benefits of clean architecture.

```
┌──────────────────────────────────────────────────────────────────┐
│                        SHELFOPS SYSTEM                           │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              Desktop Client (WPF + WebView2)              │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐ │   │
│  │  │  Shell   │  │   Web    │  │  Bridge  │  │ ApiCl.  │ │   │
│  │  │ (WPF окно)│  │ HTML/CSS │  │ C# ⇄ JS  │  │ (HTTP)  │ │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └─────────┘ │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│                           │ HTTP/JSON                           │
│  ┌────────────────────────▼─────────────────────────────────┐   │
│  │                    Web API (Kestrel)                     │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐ │   │
│  │  │Controllers│ │Middleware │ │  Auth    │  │Swagger  │ │   │
│  │  └─────┬────┘  └──────────┘  └──────────┘  └─────────┘ │   │
│  └────────┼────────────────────────────────────────────────┘   │
│           │                                                     │
│  ┌────────▼────────────────────────────────────────────────┐   │
│  │                 Application Layer                       │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐ │   │
│  │  │ Services │  │   DTOs   │  │Validators│  │Result<T>│ │   │
│  │  └─────┬────┘  └──────────┘  └──────────┘  └─────────┘ │   │
│  └────────┼────────────────────────────────────────────────┘   │
│           │                                                     │
│  ┌────────▼────────────────────────────────────────────────┐   │
│  │              Infrastructure Layer                       │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐ │   │
│  │  │   EF     │  │Repository │  │ Security │  │ Migrations│ │   │
│  │  │ DbContext│  │  Pattern  │  │ (JWT/    │  │          │ │   │
│  │  │          │  │           │  │  BCrypt) │  │          │ │   │
│  │  └─────┬────┘  └──────────┘  └──────────┘  └─────────┘ │   │
│  └────────┼────────────────────────────────────────────────┘   │
│           │                                                     │
│  ┌────────▼────────────────────────────────────────────────┐   │
│  │                   Domain Layer                         │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐ │   │
│  │  │ Entities │  │    VO    │  │  Enums   │  │Business │ │   │
│  │  │          │  │          │  │          │  │  Rules  │ │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └─────────┘ │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                   PostgreSQL (WSL)                      │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### Domain Layer (`ShelfOps.Domain`)

**Purpose:** Pure domain model with no external dependencies.

- **Entities:** `Product`, `Sale`, `SaleItem`, `CustomerReturn`, `ReturnItem`, `StockBalance`, `StockMovement`, `User`, `Category`, `Warehouse`, `AuditLog`
- **Value Objects:** `Money`, `Quantity` — type safety for monetary amounts and quantities
- **Enums:** `MovementType`, `SaleStatus`, `ReturnStatus`, `UserRole`, `RestockDisposition`, `ReferenceType`
- **Business Rules:** Domain logic encapsulated in entity methods (e.g., `Sale.CalculateSubtotal()`, `StockBalance.Decrease()`)
- **Constraints:** Entities validate their own state in constructors and public methods

### Application Layer (`ShelfOps.Application`)

**Purpose:** Orchestration of business workflows, DTO definition, validation.

- **Services:** `AuthService`, `SaleService`, `ReturnService`, `ProductService`, `StockService`, `UserManagementService`, `DashboardService`
- **Interfaces:** Repository contracts (`IProductRepository`, `ISaleRepository`...), `IUnitOfWork`
- **DTOs:** Request/response types for API communication
- **Validators:** FluentValidation validators for all DTOs
- **Common:** `Result<T>` (success/failure pattern), `PagedResult<T>` (pagination wrapper)

#### Key Design Decisions

1. **No AutoMapper** — static mapping methods in services keep transformation explicit and traceable
2. **Result<T> pattern** — services never throw for validation failures; invalid states flow through `Result.Failure`
3. **Func Injection** — `AuthService` receives `Func<string, string>` delegates for hashing rather than coupling to BCrypt directly
4. **Serialized DbContext access** — EF Core contexts are not thread-safe; all queries are awaited sequentially

### Infrastructure Layer (`ShelfOps.Infrastructure`)

**Purpose:** Data access, persistence, security infrastructure.

- **DbContext:** `ShelfOpsDbContext` — EF Core context with fluent configuration
- **Configurations:** `EntityTypeConfiguration<T>` for each entity (max length, indexes, precision, concurrency tokens)
- **Repositories:** Full implementations of all repository interfaces
- **Seed Data:** `DataSeeder` — demo dataset for development
- **Security:** `PasswordHasher` (BCrypt), `JwtTokenService`
- **Unit of Work:** Transaction wrapper around `DbContext.SaveChangesAsync`

#### Key Configuration Details

- **Concurrency:** `RowVersion` (byte[]) on `Product`, `StockBalance`, `User`
- **Money precision:** All monetary fields `decimal(18,2)`
- **Snapshots:** SaleItem stores `ProductSkuSnapshot` and `ProductNameSnapshot` as `varchar(50)` and `varchar(200)`
- **Indexes:** Composite indexes on StockMovement `(WarehouseId, ProductId)` and `(CreatedAtUtc)`
- **Cascade Delete:** Disabled by default — explicit `DeleteBehavior.Restrict`

### API Layer (`ShelfOps.Api`)

**Purpose:** HTTP entry point, authentication, routing.

- **Controllers:** 10 controllers following REST conventions under `/api/v1/`
- **Auth:** JWT Bearer with token validation parameters
- **Swagger:** OpenAPI with Bearer token security definition
- **Serilog:** Structured logging to console
- **Global Error Handling:** `ExceptionHandlingMiddleware` → ProblemDetails
- **CORS:** Open policy for desktop client (`AllowAnyOrigin`)
- **Auto-Migration:** Database created/seeded on startup in Development mode

### Desktop Client (`ShelfOps.Desktop`)

**Purpose:** Rich desktop interface using WebView2 — HTML/CSS/JS UI inside a WPF window.

- **Architecture:** Shell WPF window (`WindowStyle=None`, `WindowChrome`) hosts a full-page WebView2 — no XAML views, no ViewModels, no WPF navigation
- **UI Engine:** Vanilla JS SPA with ES modules — no frameworks, no npm, no CDN. CSS custom properties with glassmorphism (`backdrop-filter: blur`), SVG icons, animated gradient background
- **Communication:** `window.chrome.webview.postMessage` ⇄ `CoreWebView2.WebMessageReceived` ⇄ `WebBridge.cs` (C#) ⇄ `ApiClient`
- **API Client:** `ApiClient` holds the JWT Bearer token in memory (never exposed to JS). Login/Logout flows through C# bridge
- **Resources:** All HTML/CSS/JS embedded in the .exe as `EmbeddedResource` — extracted to `%LOCALAPPDATA%\ShelfOps\WebUI\` on launch, served via virtual hostname mapping (`appassets.local`)
- **Screens:** Login, Dashboard, Products, ProductCard, Sales, History, Returns, Receipts, Users, Audit Log, Settings
- **States:** Every screen handles Loading / Loaded / Empty / Error states with toast notifications

## Data Flow — Complete Sale

```
User (WPF)           API                  Application          Infrastructure       PostgreSQL
    │                  │                      │                     │                   │
    │  POST /sales/    │                      │                     │                   │
    │  complete        │                      │                     │                   │
    │ ───────────────► │                      │                     │                   │
    │                  │  CompleteSaleAsync()  │                     │                   │
    │                  │ ─────────────────────►│                     │                   │
    │                  │                       │  ExecuteTransaction │                   │
    │                  │                       │ ──────────────────►│                   │
    │                  │                       │                     │  BEGIN            │
    │                  │                       │                     │ ─────────────────►│
    │                  │                       │  GetProduct         │                   │
    │                  │                       │ ──────────────────►│───QUERY──────────►│
    │                  │                       │◄────────────────────│◄──RESULT──────────│
    │                  │                       │                     │                   │
    │                  │                       │  Check Stock        │                   │
    │                  │                       │ ──────────────────►│───QUERY──────────►│
    │                  │                       │◄────────────────────│◄──RESULT──────────│
    │                  │                       │                     │                   │
    │                  │                       │  Decrease Stock     │                   │
    │                  │                       │ ──────────────────►│───UPDATE─────────►│
    │                  │                       │                     │                   │
    │                  │                       │  Add StockMovement  │                   │
    │                  │                       │ ──────────────────►│───INSERT─────────►│
    │                  │                       │                     │                   │
    │                  │                       │  Add Sale + Items   │                   │
    │                  │                       │ ──────────────────►│───INSERT─────────►│
    │                  │                       │                     │  COMMIT           │
    │                  │                       │                     │ ─────────────────►│
    │                  │                       │◄────────────────────│                   │
    │                  │  SaleResponse ◄───────│                     │                   │
    │  ◄──────────────│                       │                     │                   │
```

## Testing Strategy

| Layer | Test Type | Tech | Count |
|---|---|---|---|
| Domain | Unit | xUnit (Moq-free) | ~50 tests |
| Application | Unit | xUnit + Moq | ~56 tests |
| API | Integration | WebApplicationFactory + InMemory | 17 tests |

## Security Model

- **Authentication:** JWT Bearer tokens (symmetric key, 8-hour expiry, 1-min clock skew)
- **Password Storage:** BCrypt (work factor determined by library default)
- **Authorization:** Role-based via `[Authorize(Roles = "...")]`
- **Idempotency:** Sales/Returns deduplication via `IdempotencyKey`
- **Audit Trail:** Every entity mutation logged via `AuditLog` with correlation ID
- **Input Validation:** FluentValidation on all public DTOs + middleware-level model state validation