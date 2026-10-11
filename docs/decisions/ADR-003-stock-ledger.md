# ADR-003: Stock Ledger Pattern

**Status:** Accepted  
**Date:** 2026-10-11  
**Deciders:** Project owner (portfolio project)  

## Context

ShelfOps tracks inventory across multiple warehouses. The system needs to:
- Show current stock levels for any product at any warehouse
- Provide a complete audit trail of stock changes
- Support multiple movement types (receipts, sales, returns, write-offs)
- Handle concurrent stock operations safely

Two patterns considered:
1. **Stock table only** — update a single QuantityOnHand column (simple, no audit trail)
2. **Ledger pattern** — StockBalance (current state) + StockMovement (event log)

## Decision

Use the **Ledger Pattern** with two tables:

- **StockBalance** — current quantity on hand per (Warehouse, Product). Optimistic concurrency via RowVersion.
- **StockMovement** — immutable log of every inventory change. Write-only, never modified.

## Consequences

### Positive
- **Full audit trail:** every stock change is permanently recorded with reference to the originating transaction (sale, receipt, return)
- **Strong consistency:** StockBalance is always derivable from StockMovement (SUM of QuantityDelta). Discrepancies are detectable and reconstructible.
- **Concurrency safety:** RowVersion on StockBalance prevents lost updates during concurrent operations
- **Temporal queries:** "What was the stock on X date?" can be answered by replaying movements up to that date
- **Forensic capability:** if stock goes negative due to a bug, the exact sequence of movements that caused it is queryable

### Negative
- **Two writes per operation:** a sale requires both a StockMovement INSERT and a StockBalance UPDATE (within a transaction)
- **Storage growth:** StockMovement grows unboundedly; for a single store with 20-30 sales/day, this is negligible (~10k rows/year)
- **Eventual consistency risk:** if StockBalance and StockMovement fall out of sync, a reconciliation procedure is needed (restore StockBalance from movement log)

### Neutral
- **Reconciliation job:** if needed, a background job can repopulate StockBalance from StockMovement — easy to implement but not needed at current scale

## Stock Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        STOCK MOVEMENT FLOW                          │
│                                                                     │
│  Receipt:  StockIn(+) → StockBalance += Qty                         │
│  Sale:     Sale(-)     → StockBalance -= Qty                         │
│  Return:   Return(+)   → StockBalance += Qty                         │
│  WriteOff: WriteOff(-) → StockBalance -= Qty                         │
│                                                                     │
│  Every movement references: ProductId, WarehouseId,                  │
│    ReferenceType/ReferenceId (source transaction),                   │
│    CreatedByUserId (who performed it)                                │
└─────────────────────────────────────────────────────────────────────┘
```

## Concurrency Model

```csharp
// StockBalance.Decrease() uses RowVersion for optimistic concurrency
public void Decrease(int quantity)
{
    if (quantity <= 0)
        throw new ArgumentException("Quantity must be positive.", nameof(quantity));
    if (QuantityOnHand < quantity)
        throw new InvalidOperationException("Insufficient stock.");
    
    QuantityOnHand -= quantity;
    UpdatedAtUtc = DateTime.UtcNow;
    // RowVersion checked by EF Core on SaveChanges
}
```

If a concurrent modification changes the stock between read and write, EF Core throws `DbUpdateConcurrencyException` — the transaction rolls back and the caller retries.

## Data Model

### StockBalance
| Column | Type | Notes |
|---|---|---|
| WarehouseId | guid | PK, FK → Warehouse |
| ProductId | guid | PK, FK → Product |
| QuantityOnHand | int | current stock level |
| UpdatedAtUtc | timestamp | last modification |
| RowVersion | uuid | EF Core concurrency token (regenerated Guid per mutation) |

### StockMovement
| Column | Type | Notes |
|---|---|---|
| Id | guid | PK |
| WarehouseId | guid | FK → Warehouse |
| ProductId | guid | FK → Product |
| MovementType | varchar(20) | StockIn / Sale / Return / WriteOff |
| QuantityDelta | int | positive for inbound, negative for outbound |
| ReferenceType | varchar(20) | Receipt / Sale / Return / Adjustment |
| ReferenceId | guid | ID of source transaction |
| Reason | text | optional explanation (required for WriteOff) |
| CreatedByUserId | guid | FK → User |
| CreatedAtUtc | timestamp | auto-set |

## Transaction Safety

The sale completion flow uses `IUnitOfWork.ExecuteTransactionAsync` to ensure atomicity:

```csharp
await _unitOfWork.ExecuteTransactionAsync(async () =>
{
    // 1. Read product + validate
    // 2. Read StockBalance + validate
    // 3. Decrease StockBalance
    // 4. INSERT StockMovement
    // 5. INSERT Sale + SaleItems
    // All or nothing
}, ct);
```

## References

- [ADR-001: Modular Monolith](ADR-001-modular-monolith.md) — overall architecture
- [Database Schema](../database-schema.md) — full table definitions
- `StockBalance.cs` — domain entity with concurrency validation
- `StockMovement.cs` — domain entity with immutability constraints
- `SaleService.CompleteSaleAsync()` — transactional stock update in action