# ADR-EXCEPTION-007: Atomic authorized override resolution and audit

- Status: Accepted
- Date: 2026-10-11
- Scope: PP-28, foundation for PP-29

## Context

UC-EXC-08 and BR-EXC-002 require every override to have an authorized actor, a reason, a valid state transition and an audit record. PP-28 owns the override flow while PP-29 owns the broader resolution and audit capability. Implementing authorization only in the UI or writing the case, resolution and audit in separate operations would permit bypasses or partial financial workflow state.

## Decision

1. The server-side IExceptionOverrideService is the command boundary. It derives tenant, actor and permissions from trusted contexts; callers provide only the case identifier and reason.
2. Exception.Override is required both by the Blazor policy and by the command service. UI visibility is not an authorization control.
3. Only Open, WaitingAction and WaitingExternal cases can be overridden. Domain code creates the Resolution and AuditEvent artifacts before applying the deterministic transition to Resolved.
4. ExceptionCase, Resolution and AuditEvent are saved by one EF Core unit of work. PostgreSQL composite foreign keys preserve tenant ownership and a unique tenant/case index prevents a second resolution.
5. PP-28 introduces only the Override resolution type and ExceptionOverride audit action. PP-29 hardens these persisted records as append-only. Additional resolution types remain deferred until product requirements define their semantics and permissions.

## Alternatives considered

- **UI-only authorization:** rejected because callers could bypass the rendered component.
- **Actor ID supplied by the form:** rejected because identity must come from the authenticated server context.
- **Separate writes for status, resolution and audit:** rejected because a failure could leave unaudited or partially resolved state.
- **Defer all persistence to PP-29:** rejected because PP-28 cannot satisfy BR-EXC-002 without an audit record.

## Consequences

- Override is a complete vertical slice and establishes the minimal Resolution/Audit persistence foundation needed by PP-29.
- The application performs one extra same-tenant actor lookup before loading the case.
- Existing users receive the new claim after bootstrap runs again and must refresh their authentication session.

## Security, tenancy, and audit impact

- Query filters hide cross-tenant cases and write validation rejects cross-tenant entities.
- Actor foreign keys include TenantId.
- Unauthorized, blank-reason, invalid-state and duplicate attempts produce no business mutation.
- The successful audit event records actor, reason, previous state, new state and UTC timestamp.

## Validation and rollback

- Domain tests cover allowed states, reason validation and duplicate resolution.
- PostgreSQL integration tests cover authorization, tenant isolation, atomic persistence and migration application.
- Rollback drops AuditEvents and Resolutions; the exception status schema remains compatible.

## Explicitly deferred

- Non-override resolution types and generic resolution orchestration: deferred until their business semantics and permission rules are specified.
- Audit timeline projection and UI: PP-30.
- Configurable logging of rejected authorization attempts.

## References

- PP-28
- UC-EXC-08, BR-EXC-002, FR-EXC-006, FR-AUD-001..003
- [ADR-IDENTITY-003](ADR-IDENTITY-003.md)
- [ADR-DATA-002](ADR-DATA-002.md)
