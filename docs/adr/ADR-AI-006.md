# ADR-AI-006: Persistence of AiAnalysis and execution telemetry for exception cases

- Status: Accepted
- Date: 2026-10-11
- Scope: PP-26

## Context

PP-25 introduced the versioned prompt and explanation service for verified price-variance exceptions. However, the explanation output and execution telemetry were transient.
To satisfy product requirements (`DM-AI`, `FR-AI-001..005`, and backlog task PP-26), the system requires persistent AI observability:
1. Auditable history of AI analyses linked to each exception case.
2. Model and prompt version traceability.
3. Observability telemetry: execution status, token usage (prompt, completion, total), and latency duration.
4. Retention of failed or unavailable attempts for operational monitoring without breaking human workflows.

## Decision

1. **Dedicated `AiAnalysis` domain entity**:
   - Model `AiAnalysis` in `ProcurePilot.Domain.Exceptions` as an entity belonging to the `ExceptionCase` aggregate (`ExceptionCase 1 -> * AiAnalysis`).
   - `AiAnalysis` implements `ITenantScopedEntity`. The PostgreSQL table `AiAnalyses` enforces a compound foreign key `(TenantId, ExceptionCaseId)` referencing `ExceptionCases(TenantId, Id)`.

2. **Persistence schema & telemetry**:
   - Persist the following attributes:
     - Identification: `Id`, `TenantId`, `ExceptionCaseId`, `AnalysisType`, `CreatedAtUtc`.
     - AI metadata: `Model`, `PromptVersion`, `ConfidenceScore`.
     - Output domains: `Summary`, `RootCauseCategory`, `RecommendedAction`, `RiskAssessment`, `RawPayloadJson`.
     - Telemetry & status: `Status` (`Success`, `Failed`, `Unavailable`), `PromptTokens`, `CompletionTokens`, `TotalTokens`, `DurationMs`, `ErrorMessage`.
   - Table constraints:
     - Check constraint: `Status IN ('Success', 'Failed', 'Unavailable')`.
     - Check constraint: `ConfidenceScore IS NULL OR (ConfidenceScore >= 0 AND ConfidenceScore <= 1)`.
     - Check constraint: `DurationMs >= 0`.

3. **OpenTelemetry instrumentation**:
   - Emit standard metrics and traces via `ActivitySource("ProcurePilot.AI")` and `Meter("ProcurePilot.AI")`:
     - `ai.analysis.count` (counter tagged with `status`, `model`, `analysis_type`)
     - `ai.analysis.duration` (histogram in milliseconds)
     - `ai.analysis.tokens.prompt`, `ai.analysis.tokens.completion`, `ai.analysis.tokens.total` (counters)

4. **Non-decision and non-blocking lifecycle**:
   - Recording an `AiAnalysis` is an advisory and observability operation; it does not mutate `ExceptionCase.Status` or alter deterministic financial figures.
   - Provider failures or disabled configurations are recorded as `Failed` or `Unavailable` without terminating user review workflows.

## Alternatives considered

- **Generic unstructured audit log table**: Rejected because AI analyses are first-class domain concepts in `DM-AI` with specific query and operational needs (retrieving the latest explanation for a case, tracking token consumption).
- **JSON blob column on `ExceptionCase`**: Rejected because an exception case can have multiple analyses over time (e.g. re-analysis after new evidence or prompt updates) and JSON columns prevent strict SQL check constraints on numeric confidence scores and telemetry metrics.
- **In-memory telemetry only (without DB persistence)**: Rejected because exit criteria for PP-26 specifically require persistent model/prompt/tokens/duration/status for offline audit and historical evaluation.

## Consequences

- Full observability of AI operations in PostgreSQL with tenant isolation.
- OpenTelemetry metrics integrate with Aspire dashboard and standard APM collectors.
- Schema changes are managed via an EF Core migration.

## References

- Backlog task: PP-26 (Persister AiAnalysis et telemetry — Sprint 6)
- Traceability: `DM-AI`, `FR-AI-001..005`
- Related ADRs:
  - [ADR-FOUNDATION-001](ADR-FOUNDATION-001.md): Solution boundaries
  - [ADR-DATA-002](ADR-DATA-002.md): PostgreSQL and EF Core migrations
  - [ADR-IDENTITY-003](ADR-IDENTITY-003.md): Tenant isolation baseline
  - [ADR-EXCEPTION-005](ADR-EXCEPTION-005.md): Typed relational evidence references
