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
