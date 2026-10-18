# PP-31 — Golden Path E2E

The required path is login → invoice upload → extraction → human review → purchase order → deterministic price match → price variance exception → work queue → evidence and grounded AI explanation → authorized override → immutable audit event.

The browser E2E starts the real Aspire AppHost with PostgreSQL and Azurite, creates an isolated tenant and administrator, and uses Chromium to sign in through the antiforgery-protected form. It uploads a PDF fixture, enters invoice details when AI extraction is disabled, confirms human review, selects the test purchase-order line and active rule, evaluates a deterministic price variance, opens the case from the work queue, checks the AI-unavailable state, and submits a justified override. It checks the rendered activity history and reads back the persisted resolution, audit event and `Resolved` state. All tenant, user, vendor, purchase-order, rule, and invoice identifiers are unique to the run; the test never depends on the local demo account.

Initial case progression is part of deterministic matching: once the failed result and factual evidence are complete, the application advances the case through the permitted `New → Analyzing → Open` transitions in the matching unit of work. The browser test does not change case state through a database fixture. An enabled AI provider is not required for the deterministic financial path; the browser verifies the visible unavailable state when AI is disabled.

Run `dotnet test tests/ProcurePilot.E2ETests/ProcurePilot.E2ETests.csproj --configuration Release`. Docker and Playwright Chromium must be available. Install the browser with `pwsh tests/ProcurePilot.E2ETests/bin/Release/net10.0/playwright.ps1 install chromium`. On a machine with Microsoft Edge installed, `PLAYWRIGHT_CHANNEL=msedge` can be used instead. CI installs Chromium and runs the test project as part of the serialized solution test command.

AI availability is not a golden-path prerequisite: the UI displays an explicit unavailable state, while matching remains deterministic and override remains permission-controlled. Tests assert that an exception's override is visible in its audit timeline.
