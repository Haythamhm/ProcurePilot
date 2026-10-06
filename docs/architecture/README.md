# Architecture documentation

Store current C4, component, data-flow, deployment, and module-boundary documentation here when it describes implemented or accepted architecture.

- Reference the governing ADRs and backlog task.
- Keep diagrams as reviewable text, preferably Mermaid.
- Update diagrams when the implementation changes.
- Do not document speculative services or claim production characteristics that have not been validated.

The current foundation decisions are indexed in [the ADR directory](../adr/README.md).

Implemented architecture notes:

- [Master data](master-data.md) — tenant-scoped Vendor baseline and demo seed.
- [Invoice domain](invoice-domain.md) — tenant-scoped Invoice aggregate and persistence baseline.
- [Invoice extraction boundary](invoice-extraction.md) — provider-neutral structured extraction contract.
- [Invoice document upload](invoice-upload.md) — validated tenant-scoped object storage and metadata flow.
- [Invoice extraction review](invoice-extraction-review.md) — auditable human correction and low-confidence gate before matching.
- [Matching rule](matching-rule.md) — tenant-scoped and versioned price-tolerance configuration.
