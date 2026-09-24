# ProcurePilot

ProcurePilot is a .NET 10 Blazor Web App orchestrated locally with Aspire.

## Prerequisites

- .NET SDK 10.0.401 or a compatible 10.0 patch selected by `global.json`
- Aspire CLI 13.5.4 (`dotnet tool install --global Aspire.Cli --version 13.5.4`)
- Docker Desktop is not required for the foundation task, but will be required when PostgreSQL is introduced in Sprint 0

## Build and test

```powershell
dotnet restore ProcurePilot.slnx
dotnet build ProcurePilot.slnx --no-restore
dotnet test ProcurePilot.slnx --no-build
```

## Run locally

```powershell
dotnet run --project ProcurePilot.AppHost
```

The AppHost starts the `web` resource and exposes the Aspire dashboard URL in the terminal.

## Current scope

This repository contains only the Sprint 0 engineering foundation. PostgreSQL, Identity, tenant resolution, CI, and business capabilities are intentionally not implemented yet.
