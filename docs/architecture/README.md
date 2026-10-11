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
- [Price variance matching](price-variance-matching.md) — deterministic, reproducible and tenant-safe line-price evaluation.
- [Exception case state machine](exception-case-state-machine.md) — tenant-scoped exception aggregate and deterministic workflow transitions.
- [Exception evidence](exception-evidence.md) — deterministic typed relational evidence references for price-variance exceptions.
- [AI chat client](ai-chat-client.md) — provider-neutral Microsoft.Extensions.AI composition root and configuration.
- [AI exception explanation](ai-exception-explanation.md) — versioned prompt and structured explanation service for price-variance exceptions.
- [AI analysis persistence and observability](ai-analysis-persistence.md) — relational persistence and OpenTelemetry execution telemetry for AI analyses.
- [Authorized exception override](exception-override.md) — permission-guarded human override with atomic resolution and audit persistence.
- [Resolution and audit retention](resolution-audit-retention.md) — append-only application guard and database retention constraints for sensitive decision records.
