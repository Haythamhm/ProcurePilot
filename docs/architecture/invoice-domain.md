# Invoice domain

## Invoice aggregate baseline — PP-10

`Invoice` is a tenant-owned transaction aggregate supplied by one `Vendor`. Its header stores the invoice number, invoice date, three-letter ISO currency code, fixed-precision total amount, and deterministic processing status. New invoices start in `Processing`; workflow transitions are intentionally outside PP-10.

`InvoiceLine` belongs to exactly one invoice and stores a positive line number, description, fixed-precision quantity, and unit price. Lines can only be created through their invoice aggregate, which propagates the tenant and invoice identifiers and rejects duplicate line numbers in memory.

PostgreSQL enforces:

- an invoice number unique for a vendor within a tenant;
- a line number unique within its invoice;
- tenant-consistent foreign keys from Invoice to Vendor and from InvoiceLine to Invoice;
- fixed precision of `numeric(18,2)` for monetary values and `numeric(18,4)` for quantity;
- restrictive Vendor deletion and aggregate-owned cascade deletion of invoice lines.

The shared EF Core tenant query filter fails closed when there is no tenant context, and the write guard rejects cross-tenant changes. Unit tests cover aggregate invariants; the Aspire/PostgreSQL integration test covers migration application, persistence with lines, read isolation, and rejected cross-tenant writes.

Document upload, extraction, corrections, matching, status transitions, API endpoints, and UI are outside PP-10.
