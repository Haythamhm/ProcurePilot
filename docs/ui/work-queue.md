# Work Queue

PP-19 introduces the tenant-scoped exception Work Queue at `/work-queue`.

## Behavior

- The server query returns only exception cases that are not `Resolved` or `Closed`.
- Tenant isolation is enforced by the existing EF Core global query filters.
- Vendor data is joined through the case invoice; a missing vendor is rendered explicitly rather than inferred.
- Cases are ordered oldest first so the oldest unresolved work remains visible.
- The page handles loading, error, empty, and populated states and uses an accessible table with a keyboard-scrollable wrapper.

## Scope boundary

PP-19 establishes the queue and its open-case read model. PP-20 owns age, severity, priority, assignment metadata, and the complete Open / Resolved / Assigned-to-me filter behavior. No placeholder financial decision or synthetic priority is generated in PP-19.
