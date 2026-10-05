# Contributing to ProcurePilot

ProcurePilot is delivered one backlog task and one vertical slice at a time. Product requirements and task status live in the Notion Delivery Hub; code, tests, migrations, and ADRs live in this repository.

## Before implementation

1. Read `AGENTS.md` and the Notion Master Handoff.
2. Select the next ready task from the Delivery Backlog.
3. Read only the specifications linked by its traceability identifiers.
4. Verify the task's Definition of Ready.
5. Inspect the current repository and the latest accepted ADRs.
6. State the scope, dependencies, risks, tests, and explicit exclusions before changing code.

Stop and report a contradiction when the specification and repository disagree. Do not silently select one interpretation.

## Architecture boundaries

The dependency direction established by `ADR-FOUNDATION-001` is:

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application / Domain
Web -> ServiceDefaults
AppHost -> Web
```

- `Domain` contains business concepts and deterministic invariants. It does not reference outer layers.
- `Application` defines use-case boundaries and abstractions. It does not reference Infrastructure or Web.
- `Infrastructure` owns EF Core, Identity, storage adapters, and external implementations.
- `Web` owns HTTP, Blazor, authentication presentation, and dependency composition.
- Add a project, framework, or infrastructure service only for a current task with a demonstrated need. Record a structural decision in an ADR.
- Do not introduce a generic repository, CQRS/MediatR, Redis, a broker, microservices, or a Worker process speculatively.

## C# conventions

- Target the SDK and framework selected by `global.json` and the project files.
- Keep nullable reference types enabled and resolve warnings rather than suppressing them globally.
- Prefer file-scoped namespaces, clear domain names, and the existing project style.
- Use asynchronous APIs for I/O and suffix asynchronous methods with `Async`.
- Accept and propagate `CancellationToken` at I/O and application boundaries when cancellation is meaningful.
- Persist timestamps in UTC and use `DateTimeOffset` when an absolute timestamp is required.
- Keep financial arithmetic, tolerances, authorization, and state transitions deterministic. AI must not decide them.
- Avoid abstractions without a concrete use in the active slice.
- Never log secrets, authentication material, or raw supplier documents.

Formatting defaults are defined in `.editorconfig`. Before committing, run:

```powershell
dotnet format ProcurePilot.slnx --verify-no-changes --no-restore
```

## Persistence and tenancy

- Keep EF Core configuration and migrations in `src/ProcurePilot.Infrastructure/Persistence`.
- Generate schema changes through the repository-local `dotnet-ef` tool and commit both migration and model snapshot.
- Application startup must not apply migrations implicitly; the Aspire `migrations` resource owns that step locally.
- Every tenant-owned business entity must carry a required `TenantId`, use the established tenant-scoped contract, and be covered by cross-tenant tests.
- The authoritative tenant comes from the authenticated server context, never directly from client input.
- Do not bypass query filters in application behavior. `IgnoreQueryFilters` is reserved for explicit infrastructure operations such as controlled provisioning and policy validation.

## Tests and quality gates

- Add unit tests for deterministic domain behavior.
- Add integration tests for EF Core, PostgreSQL, Identity, infrastructure adapters, and tenant isolation.
- Add API, component, E2E, security, or AI evaluation coverage when the active slice requires it.
- A task is not complete until its acceptance criteria pass, relevant docs are synchronized, the working tree is understandable, and CI is green.

Use the same commands as CI:

```powershell
dotnet restore ProcurePilot.slnx --locked-mode
dotnet tool restore
dotnet build ProcurePilot.slnx --configuration Release --no-restore
dotnet test ProcurePilot.slnx --configuration Release --no-build
```

Commit every generated `packages.lock.json` change with the dependency change that produced it. CI rejects lockfile drift and known vulnerable direct or transitive packages.

## ADR workflow

Create an ADR before changing a structural choice, security boundary, persistence topology, external dependency strategy, or other decision that would be expensive to reverse. Use [the ADR template](docs/adr/0000-template.md) and update [the ADR index](docs/adr/README.md).

Do not create an ADR for a local refactor that follows an accepted decision and has no durable architectural consequence.

## Git and review

- Keep branches short-lived and named by intent, for example `feat/*`, `fix/*`, or `chore/*`.
- Use Conventional Commit messages.
- Keep a commit focused on one backlog task.
- Do not mix generated migrations with unrelated formatting or refactors.
- Use a pull request for a stable milestone and ensure build and tests are green before marking the Notion task `Terminé`.
