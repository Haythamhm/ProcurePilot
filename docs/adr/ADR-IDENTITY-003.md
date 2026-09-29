# ADR-IDENTITY-003: ASP.NET Core Identity and tenant isolation baseline

- Status: Accepted
- Date: 2026-09-29
- Scope: Sprint 0 / PP-4

## Context

ProcurePilot is a B2B, multi-tenant application. PP-4 must provide login and a tenant-isolation baseline before tenant-owned business data is introduced. The approved stack requires ASP.NET Core Identity, cookie authentication, role/policy authorization, and a tenant scope calculated on the server. The domain model identifies `Tenant`, `User`, and `Role`, and favors a shared PostgreSQL database for the MVP.

## Decision

- Use ASP.NET Core Identity with `Guid` keys and secure application cookies.
- Store Identity tables, tenants, and future application tables in the existing PostgreSQL database through `ProcurePilotDbContext`.
- Represent the framework-specific user as `ApplicationUser` in Infrastructure. Each user belongs to exactly one tenant through a required foreign key.
- Keep `Tenant` as the only domain aggregate introduced by PP-4. A tenant has an explicit active/inactive status.
- Use a shared database for the MVP. Future tenant-owned entities must carry a required `TenantId` and implement the tenant-scoped marker contract.
- Resolve the current tenant from a claim created server-side from the persisted Identity user. Never accept a tenant identifier from a form, route, header, or other client input as authoritative.
- Fail closed when no valid tenant claim is available. Apply EF Core query filters to tenant-scoped entities and reject writes whose `TenantId` does not match the current tenant.
- Require an active tenant policy for authenticated application pages. Login returns the same generic error for unknown users, invalid passwords, and inactive tenants.
- Provision the first local account only through optional configuration consumed by the explicit migration resource. No default password or secret is committed.

## Consequences

- Identity schema changes are versioned in the Infrastructure migrations assembly and applied by the existing Aspire migration resource.
- A signed-in user cannot select or switch tenant through client-provided data.
- Tenant deactivation prevents new logins and the active-tenant authorization policy validates the persisted tenant state.
- The tenant query-filter and write-guard convention is ready for business entities without implementing those entities in Sprint 0.
- Shared-database isolation remains an application-layer defense. It must continue to be covered by cross-tenant integration tests as each tenant-owned vertical slice is introduced.

## Explicitly deferred

- PostgreSQL row-level security and database-per-tenant topology
- public registration, password recovery, email confirmation, MFA, and external identity providers
- user/role administration UI and detailed business permissions
- Vendor, Invoice, PurchaseOrder, ExceptionCase, and all other business entities
- audit events, business authorization policies, AI, Redis, brokers, CQRS, and MediatR
