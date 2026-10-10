# Exception evidence architecture

## Aggregate boundary — PP-22

`ExceptionCaseEvidence` is a tenant-scoped child entity of the `ExceptionCase` aggregate.
When an exception case is deterministically created from a failed price-variance match (`ExceptionCase.CreateFromFailedMatch`), exactly four factual evidence records are generated and attached to the aggregate:

1. `InvoicePrice`: references the matched invoice line (`InvoiceLineId`).
2. `PurchaseOrderPrice`: references the matched purchase order line (`PurchaseOrderLineId`).
3. `PriceVariance`: references the evaluated match result (`MatchResultId`).
4. `RuleTolerance`: references the applied matching rule (`MatchingRuleId`).

## Deterministic creation and typed references

As decided in [ADR-EXCEPTION-005](../adr/ADR-EXCEPTION-005.md):
- Evidence values are not duplicated or frozen inside `ExceptionCaseEvidence`; instead, typed foreign keys reference the canonical immutable business source entities.
- Non-duplication guarantees strict consistency with source financial entities while maintaining tenant isolation.
- Check constraints and alternate keys (`TenantId`, `Id`) enforce referential integrity at the database level.
- Multi-column check constraints ensure:
  - Valid `Type` values (`InvoicePrice`, `PurchaseOrderPrice`, `PriceVariance`, `RuleTolerance`).
  - Exactly one matching typed reference column is populated based on `Type`.

## Projections and Read-Only Workspace

- `IExceptionWorkspaceQueries` projects the four factual evidence items via read-only SQL queries scoped to the caller's active tenant.
- The Exception Workspace UI (`/exceptions/{exceptionCaseId}`) displays these factual evidence items as an informative audit block beneath the comparison layout.
- AI analysis, approval buttons, or manual mutations are excluded in compliance with the read-only projection boundaries of PP-22.
