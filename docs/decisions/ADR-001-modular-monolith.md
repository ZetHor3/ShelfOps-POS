# ADR-001: Modular Monolith Architecture

**Status:** Accepted  
**Date:** 2026-10-11  
**Deciders:** Project owner (portfolio project)  

## Context

ShelfOps is a desktop-based electronics store management system. The architectural choice affects:
- Development speed and iteration cost
- Deployment complexity
- Ability to extract services later if needed
- Testability and maintainability

Two primary options exist:
1. **Distributed microservices** — separate processes for catalog, sales, inventory, auth
2. **Modular monolith** — single deployable unit with logical layer separation

## Decision

Adopt **Modular Monolith** architecture with five logical layers (no physical process boundaries):

```
ShelfOps.Domain         — Pure domain model, no dependencies
ShelfOps.Application    — Business workflows, depends on Domain
ShelfOps.Infrastructure — Persistence/security, depends on Application
ShelfOps.Api            — HTTP entry point, depends on Infrastructure
ShelfOps.Desktop        — WPF client, network-isolated from API
```

## Consequences

### Positive
- **Simpler deployment:** single process, no orchestrator, no service mesh
- **Faster development:** no cross-service communication overhead
- **Stronger consistency:** single database, no distributed transaction complexity
- **Easier testing:** integration tests with InMemory database cover the full stack
- **Refactoring path:** layers already separated by project — extracting a service later means copying a project into a new host
- **Portfolio clarity:** demonstrates clean architecture without the operational overhead of microservices

### Negative
- **Process-level isolation:** a crash in one module takes down the whole API
- **Scaling:** cannot scale modules independently (irrelevant for a desktop store serving 1-5 concurrent cashiers)
- **Technology lock-in:** all modules share the same runtime; but different languages are unnecessary for this domain

### Neutral
- **Physical boundaries:** if the project grows beyond ~50k LOC, a bounded context can be extracted as a separate service with minimal refactoring — the project boundaries are already drawn correctly

## Alternatives Considered

### Microservices
Rejected for over-engineering at this scale. A store management system handling <10 concurrent users does not benefit from independent deployability. The operational cost (container orchestration, service discovery, observability) outweighs any architectural benefit.

### Monolithic (single project)
Rejected because it prevents clear separation of concerns. Even for a portfolio project, layered architecture demonstrates professional patterns. The Domain → Application → Infrastructure dependency flow is standard in enterprise .NET development.

## References

- [ADR-002: Desktop Client](ADR-002-desktop-client.md) — WPF as the UI technology
- [ADR-003: Stock Ledger](ADR-003-stock-ledger.md) — StockBalance + StockMovement pattern
- [Architecture Overview](../architecture.md)