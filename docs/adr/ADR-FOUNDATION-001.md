# ADR-FOUNDATION-001: .NET, Blazor, Aspire, and solution boundaries

- Status: Accepted
- Date: 2026-09-24
- Scope: Sprint 0 / PP-1

## Context

ProcurePilot needs a development foundation that supports a B2B back-office workflow while keeping deployment and operational complexity low. The product specification selects a modular monolith, Blazor Web App, .NET Aspire, and .NET 10. The repository was empty and the development machine initially exposed only .NET 9.

## Decision

- Target .NET 10 and pin SDK `10.0.401` through `global.json`.
- Use a Blazor Web App with Interactive Server rendering.
- Use a modular monolith deployed as one web process.
- Keep logical boundaries in separate projects: `Domain`, `Application`, `Infrastructure`, and `Web`.
- Use an Aspire `AppHost` to orchestrate the web project and `ServiceDefaults` for service discovery, resilience defaults, health endpoints, and OpenTelemetry wiring.
- Create unit, integration, and E2E test projects at the solution boundary. The foundation integration test starts the AppHost and verifies that the web resource becomes healthy and serves its root endpoint.
- Do not create a Worker project until a concrete background-processing use case enters scope.

## Dependency direction

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application / Domain
Web -> ServiceDefaults
AppHost -> Web
```

The Domain project does not reference outer layers. The Application project does not reference Infrastructure or Web.

## Consequences

- The initial system stays simple to run and deploy while preserving module boundaries that can be tested.
- Interactive Server keeps the first UI architecture cohesive but introduces stateful server circuits; scale-out must be tested before any horizontal-scaling claim.
- Aspire provides a single local entry point and makes later PostgreSQL wiring explicit.
- The exact SDK patch can be updated deliberately; the target framework remains `net10.0`.

## Explicitly deferred

- PostgreSQL and EF Core migrations
- ASP.NET Core Identity and tenant resolution
- background job mechanism and Worker process
- object storage and AI integrations
- production hosting choice
- microservices, Redis, message brokers, MediatR, Kubernetes, and other unproven infrastructure
