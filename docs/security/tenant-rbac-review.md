# PP-32: Tenant isolation and RBAC security review

- Status: In progress
- Review date: 2026-10-25
- Scope: `FR-TEN-001`, `FR-AUTH-001`, `FR-AUTH-002`, `UC-EXC-08`, `BR-EXC-002`
- Deliverable: security review report and regression coverage

## Scope and approach

Review server-derived tenant identity, EF Core tenant query filters and write validation, authentication and authorization policies, identity-user lookups, and the authorized exception override path. Validate cross-tenant reads and writes and ensure an unauthorized or cross-tenant actor cannot create a resolution or audit event.

Out of scope: external penetration testing, production infrastructure configuration, role-administration features, and deployment hardening assigned to other Sprint 8 tasks.

## Findings

### F-01 — Extraction reviewer lookup omitted the tenant predicate

- Severity: Low; defense-in-depth and invariant consistency.
- Location: `InvoiceExtractionReviewService.ConfirmAsync`.
- Evidence: reviewer existence had been checked using the global Identity user ID only. The database's composite tenant/user foreign key prevented a cross-tenant reviewer from being persisted, but the application boundary did not reject the mismatch explicitly.
- Remediation: include both `reviewedByUserId` and the active `tenantId` in the lookup and return the existing workspace validation error when they do not match.
- Regression coverage: integration test submits an otherwise valid review with a user from another tenant and asserts rejection before persistence.

## Controls verified

- The tenant claim is generated from the server-side Identity user; `ClaimsTenantContext` accepts exactly one valid tenant claim and fails closed for missing or ambiguous values.
- Tenant-scoped entities have EF Core query filters, and `SaveChanges` validates tenant ownership on writes.
- `ApplicationUser` is not globally query-filtered because login must resolve a user before an authenticated tenant context exists. Application lookups used for override, extraction review, and exception activity include an explicit tenant condition or a tenant-qualified join.
- Sensitive routes require an active-tenant policy and the corresponding permission claim.
- Exception override rechecks authentication, permission, actor membership in the active tenant, and tenant-scoped exception visibility inside the application service. Resolution and audit records are created together and cannot be modified or deleted after persistence.
- Integration coverage already exercises cross-tenant entity reads/writes, missing tenant context, override without permission, and override attempts from another tenant. This task adds read-filter assertions for evidence, AI analyses, resolutions, and audit events.

## Validation

- Unit tests: 133/133 passed.
- Formatting: `dotnet format ProcurePilot.slnx --verify-no-changes --no-restore` passed; `git diff --check` passed.
- Aspire/PostgreSQL integration: attempted locally, but Aspire reported the Docker runtime unhealthy before test assertions ran. The same integration test must pass in GitHub Actions before PP-32 is marked complete.
- Full CI is the merge gate. Record its result in the Delivery Backlog and complete the DoD before marking PP-32 done.

## Residual limitations

This is a code and test review, not an external penetration test. Production secure headers, rate limiting, backup/restore, deployment controls, and operational monitoring remain separate Sprint 8 work items.
