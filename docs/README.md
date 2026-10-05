# ProcurePilot documentation

This directory contains versioned technical documentation. Product requirements, UX source material, and Delivery Backlog status remain in the Notion Delivery Hub linked from `AGENTS.md`.

## Map

- [Architecture Decision Records](adr/README.md) — durable technical decisions and the reusable ADR template.
- [Architecture](architecture/README.md) — current system boundaries and diagrams derived from implemented behavior.
- [API](api/README.md) — implemented HTTP contracts, error shapes, and authentication requirements.
- [AI evaluations](ai-evals/README.md) — versioned evaluation cases and promotion evidence for AI capabilities once they enter scope.
- [Runbooks](runbooks/README.md) — operational procedures that can be executed and verified.
- [Frontend design system](ui/design-system.md) — palette, typography, icons, accessibility, and responsive rules for implemented UI surfaces.
- [Contribution rules](../CONTRIBUTING.md) — task workflow, coding conventions, tests, migrations, and security rules.

## Documentation rules

- Document implemented behavior, not speculative features.
- Link decisions to their ADR and backlog traceability identifiers where available.
- Update documentation in the same task and commit as the behavior it describes.
- Keep secrets, credentials, personal data, and supplier document contents out of the repository.
- Prefer concise Markdown and Mermaid diagrams that remain reviewable in Git.
