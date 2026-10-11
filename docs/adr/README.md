# Architecture Decision Records

ADRs record durable technical decisions that affect system boundaries, data topology, security, infrastructure, external dependencies, or choices that are expensive to reverse.

## Naming and lifecycle

- File name: `ADR-{AREA}-{NNN}.md`, using a stable area and globally unique sequence number.
- Title: `ADR-{AREA}-{NNN}: concise decision`.
- Status values: `Proposed`, `Accepted`, `Superseded`, or `Deprecated`.
- Accepted ADRs are historical records. Supersede them with a new ADR instead of rewriting the original decision.
- Use [0000-template.md](0000-template.md) for new decisions.

## Required content

An ADR must explain context, the decision, alternatives, consequences, security/tenant/audit impact, validation or rollback, explicit deferrals, and traceability references. It should be specific enough that another contributor can understand both the choice and its limits.

## Index

| ADR | Status | Scope | Decision |
| --- | --- | --- | --- |
| [ADR-FOUNDATION-001](ADR-FOUNDATION-001.md) | Accepted | PP-1 | .NET 10, Blazor, Aspire, and solution boundaries |
| [ADR-DATA-002](ADR-DATA-002.md) | Accepted | PP-2 | PostgreSQL local orchestration and EF Core migrations |
| [ADR-IDENTITY-003](ADR-IDENTITY-003.md) | Accepted | PP-4 | ASP.NET Core Identity and tenant isolation baseline |
| [ADR-STORAGE-004](ADR-STORAGE-004.md) | Accepted | PP-9 | Azure Blob-compatible invoice document storage |
| [ADR-EXCEPTION-005](ADR-EXCEPTION-005.md) | Accepted | PP-22 | Typed relational evidence references for price-variance exception cases |
