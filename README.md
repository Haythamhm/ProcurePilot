# ProcurePilot

ProcurePilot is a .NET 10 Blazor Web App orchestrated locally with Aspire.

- [Contribution and coding conventions](CONTRIBUTING.md)
- [Technical documentation](docs/README.md)
- [Architecture decisions](docs/adr/README.md)

## Prerequisites

- .NET SDK 10.0.401 or a compatible 10.0 patch selected by `global.json`
- Aspire CLI 13.5.4 (`dotnet tool install --global Aspire.Cli --version 13.5.4`)
- Docker Desktop with Linux containers

## Build and test

```powershell
dotnet restore ProcurePilot.slnx
dotnet tool restore
dotnet build ProcurePilot.slnx --no-restore
dotnet test ProcurePilot.slnx --no-build
```

## Continuous integration

The GitHub Actions workflow in `.github/workflows/ci.yml` runs on pull requests and pushes to `main`. It restores the SDK and repository-local tools, builds the complete solution in Release mode, and runs the full test suite. The GitHub-hosted Linux runner uses Docker for the real PostgreSQL integration test orchestrated by Aspire.

## Run locally

```powershell
dotnet run --project ProcurePilot.AppHost
```

The AppHost starts PostgreSQL in Docker, applies pending EF Core migrations through the dedicated `migrations` resource, then starts `web`. Aspire injects the `procurepilot` connection string; no database password or fixed host port is stored in the repository. The dashboard URL is printed in the terminal.

The AppHost runs the web resource with its HTTP launch profile so orchestration and CI do not depend on trusting a machine-specific development certificate. HTTPS remains available when the Web project is launched directly with its HTTPS profile.

## Provision a local login

ProcurePilot has no public registration flow. Configure the first local tenant administrator through AppHost user secrets:

```powershell
dotnet user-secrets set --project ProcurePilot.AppHost "Identity:Bootstrap:TenantName" "Local Demo"
dotnet user-secrets set --project ProcurePilot.AppHost "Identity:Bootstrap:Email" "admin@local.test"
dotnet user-secrets set --project ProcurePilot.AppHost "Identity:Bootstrap:Password" "choose-a-strong-local-password"
dotnet user-secrets set --project ProcurePilot.AppHost "Identity:Bootstrap:DisplayName" "Local Admin"
```

Start the AppHost normally. The explicit `migrations` resource applies the schema and idempotently creates the configured tenant, user, `Administrator` role, and the synthetic `Atlas Manufacturing` demo vendor before `web` starts. The vendor is scoped to the configured bootstrap tenant. If all four values are absent, no account or demo vendor is created. A partial configuration fails fast. No credentials are stored in the repository.

Open `/account/login`. The application uses an HttpOnly Identity cookie, antiforgery protection, and a server-generated tenant claim. Production cookies are always marked Secure; local Aspire HTTP uses the development exception required by the local launch profile.

## Create a migration

Run this only after deliberately changing the EF Core model:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> `
  --project src/ProcurePilot.Infrastructure `
  --output-dir Persistence/Migrations
```

Commit the generated migration and model snapshot. Starting the AppHost then applies it to PostgreSQL before the web application starts. The normal web process never applies migrations implicitly.

## Current scope

This repository contains the completed Sprint 0 engineering foundation and the PP-6 Vendor master-data baseline for Sprint 1. Purchase orders, invoices, matching, exceptions, AI, and other later capabilities are intentionally not implemented yet.
