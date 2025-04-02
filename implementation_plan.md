# TradeView — Implementation Plan

**TradeView | Market Data REST API**
C#, .NET 8, ASP.NET Core, Entity Framework Core, xUnit

A REST API backend for real-time market data tracking. It ingests price ticks for a
set of instruments (from a pluggable, simulated feed), stores them with EF Core,
exposes quote/history endpoints, tracks trades to compute realized and unrealized
P&L, and evaluates user-configured price alerts as new prices arrive.

## Key design decisions

- **Layered solution** — `TradeView.Core` (domain + pure services, no EF/ASP.NET
  dependencies), `TradeView.Infrastructure` (EF Core `DbContext`, repositories, market
  data feed), `TradeView.Api` (controllers, DTOs, hosting), `TradeView.Tests` (xUnit).
- **Database** — SQLite by default (zero setup) via EF Core; connection string from
  configuration / `ConnectionStrings__TradeView` env var. Schema created with
  `EnsureCreated` on startup to keep the project self-contained (migrations optional).
- **Market data** — `IMarketDataFeed` abstraction. Default implementation is a
  geometric random-walk simulator so the API runs offline with no API keys. A
  `BackgroundService` polls the feed on a configurable interval, persists ticks, and
  hands each tick to the alert evaluator.
- **P&L** — average-cost method. Buys raise quantity and blend cost; sells realize
  `(price - avgCost) * qty` minus fees. Unrealized P&L uses the latest quote. Pure
  function in Core so it is unit testable.
- **Alerts** — conditions `Above`, `Below`, `PercentChange` (vs. reference price at
  creation). One-shot by default (deactivate after trigger) with optional
  `Repeating` + cooldown. Triggered events persisted to an `AlertTrigger` table.
- **Async everywhere** — all controller actions and repository methods are `async`
  with `CancellationToken` propagation.
- **Errors** — ProblemDetails via a global exception handler; domain validation
  errors map to 400, not-found to 404.

## Phases

### Phase 1 — Solution scaffold, domain model, persistence
Deliverables: solution + 4 projects, domain entities, `TradeViewDbContext` with entity
configuration, DI wiring, config files.
Files:
- `TradeView.sln`, `Directory.Build.props`
- `src/TradeView.Core/TradeView.Core.csproj`, `Domain/*.cs` (Instrument, PriceTick, Trade, PriceAlert, AlertTrigger, enums)
- `src/TradeView.Infrastructure/TradeView.Infrastructure.csproj`, `Persistence/TradeViewDbContext.cs`, `Persistence/Configurations/*.cs`, `DependencyInjection.cs`
- `src/TradeView.Api/TradeView.Api.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`
- `.env.example`

### Phase 2 — Instruments & market data ingestion
Deliverables: repository interfaces in Core, EF repositories, simulated feed,
ingestion background service, instrument + quote endpoints (latest quote, history
with time-range / limit, OHLC summary), seed instruments.
Files:
- `src/TradeView.Core/Abstractions/*.cs` (IInstrumentRepository, IPriceTickRepository, IMarketDataFeed, IClock)
- `src/TradeView.Infrastructure/Repositories/*.cs`, `MarketData/SimulatedMarketDataFeed.cs`, `MarketData/MarketDataOptions.cs`, `Persistence/DbSeeder.cs`
- `src/TradeView.Api/BackgroundServices/MarketDataIngestionService.cs`
- `src/TradeView.Api/Controllers/InstrumentsController.cs`, `QuotesController.cs`, `Contracts/*.cs`

### Phase 3 — Trades & P&L calculation
Deliverables: trade recording endpoints, `PnlCalculator` (average cost, realized +
unrealized, per-instrument and portfolio totals), `PortfolioService`, P&L endpoints.
Files:
- `src/TradeView.Core/Services/PnlCalculator.cs`, `Services/PortfolioService.cs`, `Models/PositionSummary.cs`, `Models/PortfolioSummary.cs`
- `src/TradeView.Core/Abstractions/ITradeRepository.cs`, `src/TradeView.Infrastructure/Repositories/TradeRepository.cs`
- `src/TradeView.Api/Controllers/TradesController.cs`, `PortfolioController.cs`, `Contracts/TradeContracts.cs`

### Phase 4 — Configurable price alerts
Deliverables: alert CRUD endpoints, `AlertEvaluator` (pure rule evaluation incl.
cooldown/repeat), `AlertService` invoked by the ingestion service, triggered-alert
history endpoint.
Files:
- `src/TradeView.Core/Services/AlertEvaluator.cs`, `Services/AlertService.cs`, `Abstractions/IAlertRepository.cs`
- `src/TradeView.Infrastructure/Repositories/AlertRepository.cs`
- `src/TradeView.Api/Controllers/AlertsController.cs`, `Contracts/AlertContracts.cs`
- `src/TradeView.Api/BackgroundServices/MarketDataIngestionService.cs` (hook alerts)

### Phase 5 — Hardening & xUnit test suite
Deliverables: global ProblemDetails exception handling, domain exceptions, health
check, Swagger docs; xUnit tests for `PnlCalculator`, `AlertEvaluator`,
`PortfolioService`, `AlertService` (EF Core InMemory/SQLite in-memory), and the
simulated feed.
Files:
- `src/TradeView.Core/Exceptions/*.cs`, `src/TradeView.Api/Infrastructure/GlobalExceptionHandler.cs`
- `tests/TradeView.Tests/TradeView.Tests.csproj`, `tests/TradeView.Tests/**/*.cs`

Then: `.gitignore`, `README.md` (architecture diagram, run + test instructions).
