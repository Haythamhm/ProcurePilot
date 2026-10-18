# ADR-TESTING-008: Browser-driven E2E coverage for PP-31

- Status: Accepted
- Date: 2026-10-18
- Scope: PP-31 — E2E Golden Path

## Context

The existing PP-31 test starts the real Aspire application and verifies authentication and access to the protected invoice-upload route over HTTP. It does not execute Blazor interactions such as selecting a file, reviewing extracted fields, initiating a match, or resolving an exception. HTTP assertions cannot establish that these interactive transitions work in a real browser.

## Decision

Add the official Microsoft Playwright .NET library to the E2E test project and run a single Chromium browser for the golden-path test. Keep Aspire as the system-under-test host and seed isolated tenant data inside the test. Install only Chromium in CI. Do not add browser automation to production dependencies.

## Alternatives considered

- Continue with HTTP-only tests: fast, but cannot exercise the interactive Blazor UI or file input.
- Add a browser test SaaS: introduces external credentials, network dependency, and tenant data exposure without a need.
- Use JavaScript Playwright separately: duplicates the existing .NET test harness and complicates AppHost lifecycle management.

## Consequences

- E2E test execution requires Docker, the pinned Playwright Chromium build, and additional CI time.
- Browser binaries are installed only for the E2E test job; production images and runtime remain unchanged.
- Tests must create unique tenant/user/business data and clean up through isolated ephemeral Aspire resources; no local demo identity may be assumed.
- AI-provider availability is not a test prerequisite. The golden path verifies the explicit unavailable state when AI is disabled; deterministic matching and authorization remain application-controlled.

## Security, tenant, and audit impact

The browser test signs in through the real antiforgery-protected form, uses a unique test tenant, and never introduces production credentials. Financial calculations continue through the existing deterministic matching service. Override remains guarded by the existing permission policy and audit service. Playwright is confined to test scope.

## Validation and rollback

Run the E2E project against Aspire with Docker and run the same project in CI after installing Chromium. If browser automation becomes unreliable or disproportionately expensive, the package and browser-install step can be removed while retaining the existing HTTP entry test; that would reduce PP-31 coverage and must be reflected in the task status.

## Traceability

- Product Backlog: PP-31, E2E Golden Path
- Official Playwright .NET documentation: https://playwright.dev/dotnet/docs/library
- Official Playwright browser installation documentation: https://playwright.dev/dotnet/docs/browsers
