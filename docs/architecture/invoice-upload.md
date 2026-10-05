# Invoice document upload

## PP-9 vertical slice

An authenticated user with the `Invoice.Upload` permission selects one PDF, JPEG, or PNG document of at most 10 MB. The application validates the extension, declared MIME type, actual size, and file signature before storage.

The upload service creates:

- a tenant-scoped `Invoice` in `Processing`, with extracted header fields intentionally absent;
- a tenant-scoped `InvoiceDocument` with original file name, normalized content type, size, SHA-256, uploader, UTC upload time, and generated storage reference;
- a private blob under `{tenant}/{invoice}/{document}.{extension}`.

The blob is written before the PostgreSQL transaction. If persistence fails, the service attempts to delete the blob. The original file name never participates in the storage key. Tenant query filters and write guards apply to both records.

The `/invoices/upload` route requires an active tenant and the `Invoice.Upload` permission. The bootstrap `Administrator` role receives that permission idempotently.

## Boundaries

The slice does not run extraction, infer suppliers or financial values, create invoice lines, perform matching, or make financial decisions. Those remain later Sprint 2 tasks.

Storage provider rationale and deferred hardening are recorded in [ADR-STORAGE-004](../adr/ADR-STORAGE-004.md).
