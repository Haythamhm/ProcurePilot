# Work Queue

PP-19 introduces the tenant-scoped exception Work Queue at `/work-queue`; PP-20 completes its prioritization metadata and MVP views.

## Behavior

- URL-backed views expose open (`view=open`), resolved (`view=resolved`), and active cases assigned to the authenticated user (`view=mine`).
- All filtering, including current-user assignment, runs server-side.
- Tenant isolation is enforced by the existing EF Core global query filters.
- Vendor data is joined through the case invoice; a missing vendor is rendered explicitly rather than inferred.
- Cases are ordered oldest first so the oldest unresolved work remains visible.
- Age is derived at render time from `OpenedAtUtc` through `TimeProvider`; no stale age column is persisted.
- Priority displays the required `ExceptionSeverity` value. The initial `PRICE_VARIANCE` severity comes from validated `ExceptionManagement` configuration.
- The page handles loading, error, empty, and populated states and uses an accessible table with a keyboard-scrollable wrapper.

## Scope boundary

`AssignedToUserId` is intentionally minimal P0 metadata for queue filtering. Assignment commands, comments, notifications, and reassignment workflow remain P1. No severity is inferred by AI or calculated from an undocumented financial threshold.
