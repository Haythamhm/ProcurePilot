# ADR-STORAGE-004: Store invoice documents in Azure Blob-compatible object storage

- Status: Accepted
- Date: 2026-10-05
- Scope: Sprint 2 / PP-9

## Context

PP-9 requires PDF and image invoices to be stored outside the relational database while PostgreSQL retains tenant-scoped metadata and a storage reference. The implemented Aspire baseline needs a deterministic local development and integration-test equivalent with a direct production path and no repository-held secret.

## Decision

Use Azure Blob Storage through the official Azure SDK. The Aspire AppHost runs Azurite with a named volume locally and exposes the `invoiceblobs` connection to the web application. Production can supply an Azure Blob connection through configuration without changing the application boundary.

The application owns `IInvoiceDocumentStorage`; Infrastructure owns the Azure implementation. Original bytes are stored in the private `invoice-documents` container under a generated tenant/invoice/document key. PostgreSQL stores `InvoiceDocument` metadata, SHA-256, uploader, UTC timestamp, and storage reference. It never stores the document bytes.

## Alternatives considered

- **Relational byte array:** rejected because it conflicts with the domain-model constraint and couples database growth and backup operations to unstructured files.
- **Local filesystem:** rejected because it is not portable across application instances and has no clean production parity.
- **S3-compatible storage:** valid but not selected because the approved target deployment already identifies Azure and the official Aspire integration provides Azurite for local parity.

## Consequences

- Local development and CI require Docker for both PostgreSQL and Azurite.
- Blob upload happens before the relational commit; a failed commit triggers best-effort blob deletion.
- The storage abstraction permits a later provider change without leaking Azure types into Domain or Application.
- Operations must manage object lifecycle, backup, and retention policies in the deployment environment.

## Security, tenancy, and audit impact

The container has no public access. Blob keys begin with a server-derived tenant identifier and contain no supplier file name. Upload authorization requires `Invoice.Upload` plus an active tenant. File type is checked using extension, declared MIME type, size, and magic bytes. Metadata records the uploader, UTC time, SHA-256, and tenant. Supplier bytes remain untrusted and are not interpreted in PP-9.

## Validation and rollback

Unit tests cover the processing invoice and metadata invariants. The Aspire integration test applies the migration, uploads a PDF to Azurite, verifies the private blob and PostgreSQL metadata, and verifies tenant query isolation. Rollback requires preserving or exporting stored documents before reversing the schema migration.

## Explicitly deferred

- Malware scanning and content disarm/reconstruction.
- Extraction execution and retry orchestration.
- Human extraction review and correction.
- Cloud retention, legal hold, and customer-managed encryption keys.

## References

- Backlog task: PP-9 — Implémenter upload document facture
- Requirements / use cases: UC-INV-01, FR-INV-001..003
- Related ADRs: ADR-FOUNDATION-001, ADR-DATA-002, ADR-IDENTITY-003
