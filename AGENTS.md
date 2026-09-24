# ProcurePilot AI — Agent Instructions

## Sources of truth

Product requirements and backlog:
https://app.notion.com/p/ProcurePilot-AI-Cahier-des-charges-Delivery-Hub-3e52751845fe814eac30e61eb6419278?source=copy_link

UI/UX:
https://www.figma.com/design/03tCdpmNtEl02NJ97xCPzH

FigJam — Use Cases
https://www.figma.com/board/g1kVHqyybxBZylbvOzdLCx/ProcurePilot---Use-Cases?node-id=0-1&t=Uoamel1nPSqSItcR-1

FigJam — Exception Resolution Flow
https://www.figma.com/board/KtOZw1JvPlaHjyC1iWNOUk/ProcurePilot---Exception-Resolution-Flow?node-id=0-1&t=qyFKEAv5b2miOkLB-1

## Working rules

- Read the Notion Master Handoff before implementation.
- Work from the Delivery Backlog.
- Implement one task / vertical slice at a time.
- Respect P0 / P1 / P2 / Later scope.
- Do not invent features.
- Do not change architecture silently.
- Do not add frameworks or infrastructure without a justified need and ADR.
- Financial arithmetic, tolerances, authorization and state transitions must be deterministic.
- AI may extract, explain, retrieve and recommend, but must not make sensitive financial decisions.
- Preserve tenant isolation, authorization and auditability.
- Check Definition of Ready before starting a task.
- Check Definition of Done before marking a task complete.
- Keep tests and documentation synchronized with implementation.
