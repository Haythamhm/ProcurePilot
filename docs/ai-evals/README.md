# AI Evaluation Documentation

This directory documents versioned AI evaluation cases, regression benchmarks, and promotion evidence for ProcurePilot AI capabilities.

## Principles & Guardrails
- **Separate deterministic expected facts from qualitative scoring**: Factual keyword presence, price grounding, and forbidden patterns are evaluated deterministically.
- **Record model/provider, prompt version, dataset version, and evaluation date**: Full provenance is captured in `AiEvalResult`.
- **Release blockers**: Cross-tenant disclosure, invented financial evidence, and autonomous sensitive actions are classified as critical hallucinations and block promotion.
- **Synthetic fixtures**: Never commit secrets, personal data, or raw supplier documents. All evaluation cases use synthetic, reproducible fixtures.

## Datasets & Eval Packs

| Dataset | Version | Traceability | Scope & Focus |
|---|---|---|---|
| [REF-001 to REF-006](dataset-ref-001-006.md) | `1.0.0` | `AT-AI-001` (PP-27) | Price variance exceptions, critical hallucination detection, prompt injection resistance, and non-decision boundary enforcement |
