# ADR-DATA-002: PostgreSQL local orchestration and EF Core migrations

- Status: Accepted
- Date: 2026-09-29
- Scope: Sprint 0 / PP-2

## Context

ProcurePilot needs a reproducible local relational database before business models are introduced. The approved stack selects PostgreSQL, EF Core 10, Npgsql, Docker, and Aspire. PP-2 must establish persistence and migrations without anticipating Identity, tenant isolation, or business entities.

## Decision

- Run PostgreSQL locally as an Aspire resource backed by Docker and a named data volume.
- Create the logical database resource `procurepilot` beneath the `postgres` server resource.
- Let Aspire inject `ConnectionStrings__procurepilot` into dependent project resources through `WithReference`.
- Keep `ProcurePilotDbContext` and all EF Core configuration in `ProcurePilot.Infrastructure/Persistence`.
- Keep migrations in `ProcurePilot.Infrastructure/Persistence/Migrations`, in the same assembly as the context.
- Use `Npgsql.EntityFrameworkCore.PostgreSQL` as the EF Core provider.
- Keep schema changes explicit and versioned. A dedicated Aspire `migrations` project resource runs the Web host with `--migrate`; the normal Web resource waits for it to finish successfully.
- Pin `dotnet-ef` in the repository tool manifest so migration generation uses a reproducible tool version.
- Start with an empty technical baseline migration. Applying it creates only EF Core migration history; no business, Identity, or tenant tables enter PP-2.

## Consequences

- `dotnet run --project ProcurePilot.AppHost` provisions PostgreSQL, applies pending migrations once, then starts the Web resource.
- The same migration set can be applied to a fresh PostgreSQL database and re-applied idempotently.
- Application startup in normal Web mode does not mutate the schema.
- The migration resource currently reuses the Web executable to avoid introducing a permanent Worker or a second deployment unit. This can be separated later if deployment requirements justify it.
- Database credentials and the mapped host port are owned by Aspire rather than committed configuration.

## Explicitly deferred

- Identity schema and authentication
- tenant model, query filters, row-level security, and database-per-tenant decisions
- Vendor, Invoice, PurchaseOrder, ExceptionCase, and all other business entities
- pgvector enablement, Redis, brokers, generic repositories, CQRS, and MediatR
- production database hosting, backup/restore implementation, and deployment-time migration topology
