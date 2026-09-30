# Master data

## Vendor baseline — PP-6

`Vendor` is tenant-owned master data stored in PostgreSQL. The initial model contains:

- `Id` — application-generated identifier;
- `TenantId` — required owner and foreign key to `Tenant`;
- `ExternalReference` — required, unique within a tenant;
- `Name` — required display name;
- `TaxIdentifier` — optional external tax identifier.

EF Core applies the shared tenant query filter and write guard established by ADR-IDENTITY-003. Reads fail closed without a tenant context, and writes for another tenant are rejected. Deleting a tenant does not cascade to vendors.

The explicit Aspire `migrations` resource creates the synthetic `Atlas Manufacturing` vendor after provisioning the configured bootstrap tenant and account. The seed is idempotent, uses `ATLAS-MFG` as its stable external reference, and refuses to overwrite conflicting data. No vendor CRUD, API, or UI is part of PP-6.

## Purchase order baseline — PP-7

`PurchaseOrder` is a tenant-owned transaction aggregate supplied by one `Vendor`. Its header stores the PO number, order date, ISO currency code, status text, and fixed-precision total amount. `PurchaseOrderLine` stores a positive line number, description, fixed-precision quantity, and unit price.

The database enforces:

- a PO number unique within a tenant;
- a line number unique within its purchase order;
- tenant-consistent foreign keys from PO to Vendor and from line to PO;
- fixed precision of `numeric(18,2)` for monetary values and `numeric(18,4)` for quantity;
- restrictive Vendor deletion and aggregate-owned cascade deletion of PO lines.

The migration resource seeds the synthetic `PO-2026-1042` with one line after Atlas exists. The seed is idempotent and rejects conflicting data. Status lifecycle rules, PO CRUD, API, and UI remain outside PP-7.
