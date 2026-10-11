# ADR-EXCEPTION-005: Typed relational evidence references for price-variance exception cases

- Status: Accepted
- Date: 2026-10-10
- Scope: PP-22

## Context

When automated 3-way price-variance matching fails, ProcurePilot creates an `ExceptionCase` to track human investigation. To provide auditability, transparency, and immediate operational clarity in the Exception Workspace, the system must capture exactly four factual evidence points:
1. Invoice unit price
2. Purchase order unit price
3. Evaluated price variance
4. Applied tolerance rule and threshold

A structural decision is needed on how evidence is modeled, persisted, and projected.

## Decision

1. **Dedicated entity with typed relational references**:
   - Model `ExceptionCaseEvidence` in `ProcurePilot.Domain.Exceptions` as an entity belonging to the `ExceptionCase` aggregate.
   - For each failed price-variance match, exactly four evidence records are generated deterministically upon case creation:
     - `InvoicePrice`: references `InvoiceLineId`
     - `PurchaseOrderPrice`: references `PurchaseOrderLineId`
     - `PriceVariance`: references `MatchResultId`
     - `RuleTolerance`: references `MatchingRuleId`
   - Each evidence record carries an explicit foreign key to its target entity, enforced at the database level with `DeleteBehavior.Restrict`.

2. **No duplicated financial values**:
   - Financial numbers (amounts, unit prices, percentages) remain strictly within their source entities (`InvoiceLine`, `PurchaseOrderLine`, `MatchResult`, `MatchingRule`).
   - Evidence records hold typed relational pointers and metadata (`Type`, `RecordedAtUtc`), preventing drift or duplicate representations of truth.

3. **Projection in Exception Workspace**:
   - `IExceptionWorkspaceQueries` projects the four evidence records alongside the case context in a single read-only server projection.
   - The UI displays these facts semantically without invoking AI and without computing or altering financial decisions.

## Alternatives considered

- **JSON snapshot column on ExceptionCase**: Rejected because it denormalizes financial figures, bypasses relational foreign key integrity, and risks schema drift without migration safety.
- **Generic polymorphic reference (`EntityId`, `EntityType`)**: Rejected because PostgreSQL cannot enforce relational foreign key constraints on polymorphic columns, breaking database-level referential integrity.
- **Dynamic query-time inference without persistence**: Rejected because matching rules and lines can theoretically be archived or updated over time; auditability requires an explicit, permanent record of what facts justified the exception when it was opened.

## Consequences

- Exactly four immutable evidence records are persisted per price-variance case in the same database unit of work.
- Referential integrity is protected by PostgreSQL foreign keys and tenant isolation filters.
- Queries require relational joins, handled efficiently via `AsNoTracking()` projections.
- Future exception types (e.g., quantity variance) can introduce corresponding evidence types without redesigning the schema.

## Security, tenancy, and audit impact

- `ExceptionCaseEvidence` implements `ITenantScopedEntity`. All foreign keys and query filters enforce `TenantId`.
- Read access is governed by the existing `AuthorizationPolicies.ExceptionView` policy.
- Full auditability is achieved: an auditor can trace every exception back to the exact source invoice line, PO line, evaluation timestamp, and versioned matching rule.

## Validation and rollback

- Covered by unit tests in `ExceptionCaseTests` and integration tests in `AppHostTests`.
- Schema changes are managed via a dedicated EF Core migration.
- Reversal would require deprecating the `ExceptionCaseEvidences` table via an ADR and migration.

## Explicitly deferred

- Evidence attachment for other exception types (e.g. quantity variance, missing goods receipt) is deferred to future backlog tasks.
- Manual evidence uploads, user comments, and dispute attachments remain P1/P2 scope.
- Automated resolution or approval actions based on evidence remain strictly out of scope.

## References

- Backlog task: PP-22 (Preuves factuelles d'exception)
- Related ADRs:
  - [ADR-FOUNDATION-001](ADR-FOUNDATION-001.md): Solution boundaries
  - [ADR-DATA-002](ADR-DATA-002.md): PostgreSQL and EF Core migrations
  - [ADR-IDENTITY-003](ADR-IDENTITY-003.md): Tenant isolation baseline
