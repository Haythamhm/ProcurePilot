# Resolution and audit retention

PP-29 completes the persistence guarantees for the Resolution and AuditEvent records introduced by PP-28.

## Write policy

- New Resolution and AuditEvent records are created only as part of the authorized override transaction.
- ProcurePilotDbContext rejects Modified and Deleted states for either entity before saving.
- Added records remain allowed so the existing atomic override flow continues to work.
- Tenant write validation still runs for every tenant-scoped change.

This guard protects the normal application path used by business roles. Direct database administration remains an operational privilege outside the business application boundary.

## Retention

The composite tenant-scoped foreign keys from Resolution and AuditEvent to ExceptionCase use restrictive deletion. An audited case therefore cannot be cascade-deleted with its decision history.

The migration `EnforceResolutionAuditRetention` replaces the previous cascade behavior. Its rollback restores the earlier foreign keys.

## Scope boundary

The product specification currently defines Override as the only concrete resolution type. PP-29 does not invent additional financial outcomes or permissions. The chronological activity projection and UI belong to PP-30.

## Verification

PostgreSQL integration coverage proves that:

- the authorized override can still create both records atomically;
- changing a persisted Resolution is rejected;
- deleting a persisted AuditEvent is rejected;
- deleting the parent ExceptionCase is rejected while retained decision records exist.
