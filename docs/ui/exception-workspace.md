# Exception Workspace

PP-21 introduces the read-only, tenant-scoped exception workspace at `/exceptions/{exceptionCaseId}`.

## Behavior

- A single no-tracking server projection loads the exception case, matched invoice line, purchase-order line, persisted match result, matching-rule version, supplier, optional source filename, and optional assignee.
- Global EF Core query filters keep every joined tenant-scoped entity inside the active tenant. An unknown or cross-tenant identifier returns the same not-found state and does not disclose whether the case exists elsewhere.
- The route requires the `Exception.View` permission in addition to an authenticated active tenant. The bootstrap Administrator role receives this permission.
- Financial display uses persisted match-result prices, signed variance, and tolerance. The UI does not recompute the match decision and does not call AI.
- The comparison presents facts in the order invoice → deterministic mismatch → purchase order, then shows operational ownership and lifecycle timestamps.
- Factual evidence (PP-22) displays the four deterministic price and tolerance records captured at match failure, referencing their source entities without duplicating values.
- Loading, retryable error, not-found, unassigned, missing source filename, open, and resolved states are explicit.
- The three-column desktop comparison reflows to a variance-first two-column view and then a single column for narrow screens.

## Scope boundary

AI analysis, comments, activity history, assignment commands, escalation, override, and resolution actions are intentionally excluded. Those capabilities belong to later backlog slices and are not represented by inactive or hard-coded controls.
