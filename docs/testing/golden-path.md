# PP-31 — Golden Path E2E

The required path is login → invoice upload → extraction → human review → purchase order → deterministic price match → price variance exception → work queue → evidence and grounded AI explanation → authorized override → immutable audit event.

The first executable E2E slice starts the real Aspire AppHost with PostgreSQL and Azurite, creates an isolated tenant and operator, checks that anonymous access to invoice upload requires sign-in, then signs in through the real antiforgery-protected form and reaches the protected upload page. The test uses a unique tenant and email per run and never depends on the local demo account.

Run `dotnet test tests/ProcurePilot.E2ETests/ProcurePilot.E2ETests.csproj --configuration Release`. Docker must be available. The CI solution test command includes this project.

The remaining browser path is not yet covered. At this stage the review page says matching is not started, while the deterministic matching service is exercised separately by integration tests; the workspace also has no grounded AI explanation display. PP-31 cannot meet its full exit criterion until those user-facing transitions are available and covered by an interactive browser test. Do not mark this task done based on the entry test alone.
