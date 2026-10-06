# Matching rule

## Versioned price-tolerance rule — PP-14

`MatchingRule` is tenant-scoped master data. A rule stores its type, allowed tolerance percentage, monotonically increasing version, and UTC effective interval. The tolerance is persisted as fixed-precision decimal data; it is never sourced from UI constants or delegated to AI.

Creating a new version closes the current rule at the next version's effective timestamp and preserves the historical tolerance. PostgreSQL enforces one version number per tenant and rule type, plus at most one open-ended current rule per tenant and type. This makes later `MatchResult` records reproducible against the exact rule version used.

PP-14 intentionally does not execute matching or expose a rule-administration UI. PP-15 consumes this rule to calculate and persist deterministic price-variance results; see [Price variance matching](price-variance-matching.md).

Tenant query filters fail closed without an active tenant, and the shared write guard rejects cross-tenant changes.
