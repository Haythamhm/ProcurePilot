# Authorized exception override

PP-28 adds a human-controlled resolution action to the Exception Workspace.

## Request boundary

The UI submits only the exception case identifier and the human reason. IExceptionOverrideService obtains the active tenant, authenticated user and Exception.Override permission from server contexts. It never trusts an actor or tenant identifier supplied by a browser.

## Transaction

For an eligible case (Open, WaitingAction or WaitingExternal), the domain creates:

- one Resolution with type Override;
- one AuditEvent with action ExceptionOverride;
- one deterministic transition to Resolved.

EF Core persists all three changes in one SaveChangesAsync call. Composite tenant foreign keys and the unique resolution index prevent cross-tenant references and duplicate resolution.

## Failure behavior

- Missing permission returns Forbidden before the case is loaded.
- A blank or oversized reason returns InvalidReason.
- A hidden cross-tenant case returns no data or mutation.
- Invalid or already resolved states return InvalidState.
- Unexpected persistence failures are surfaced to the UI as a non-success state; no partial transaction is accepted.

## PP-29 boundary

PP-29 makes persisted Resolution and AuditEvent records immutable through the normal EF Core application path and changes their case foreign keys from cascade to restrict. It reuses the entities and transaction principles introduced here rather than creating a parallel resolution model. New resolution types remain out of scope until their business semantics and authorization rules are specified.
