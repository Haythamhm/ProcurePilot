using System.Net;
using System.Text.RegularExpressions;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Infrastructure;
using ProcurePilot.Infrastructure.Identity;
using ProcurePilot.Infrastructure.Persistence;
using ProcurePilot.Web.Identity;
using ProcurePilot.Domain.Matching;
using ProcurePilot.Domain.Purchasing;
using ProcurePilot.Domain.Vendors;
using ProcurePilot.Domain.Exceptions;
using Microsoft.Playwright;

namespace ProcurePilot.E2ETests;

public sealed class InvoiceGoldenPathTests
{
    [Fact]
    public async Task UserUploadsReviewsMatchesOverridesAndAuditsException()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var cancellationToken = timeout.Token;
        var runId = Guid.NewGuid().ToString("N");
        var email = $"golden-{runId}@integration.local";
        const string password = "ProcurePilot!42";

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ProcurePilot_AppHost>(cancellationToken);
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("web", cancellationToken);

        var database = await app.GetConnectionStringAsync("procurepilot", cancellationToken);
        var blobStorage = await app.GetConnectionStringAsync("invoiceblobs", cancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(database));
        Assert.False(string.IsNullOrWhiteSpace(blobStorage));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddScoped<ITenantContext>(_ => new TestTenantContext());
        services.AddInfrastructure(database, blobStorage);
        await using (var provider = services.BuildServiceProvider())
        await using (var scope = provider.CreateAsyncScope())
        {
            var bootstrapper = new IdentityBootstrapper(
                Options.Create(new IdentityBootstrapOptions
                {
                    TenantName = $"Golden path {runId}",
                    Email = email,
                    Password = password,
                    DisplayName = "Golden path operator"
                }),
                scope.ServiceProvider.GetRequiredService<ProcurePilotDbContext>(),
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
            await bootstrapper.SeedAsync(cancellationToken);
        }

        Guid tenantId;
        {
            var tenantServices = new ServiceCollection();
            tenantServices.AddLogging();
            tenantServices.AddScoped<ITenantContext>(_ => new TestTenantContext());
            tenantServices.AddInfrastructure(database!, blobStorage!);
            await using var tenantProvider = tenantServices.BuildServiceProvider();
            await using var tenantScope = tenantProvider.CreateAsyncScope();
            var context = tenantScope.ServiceProvider.GetRequiredService<ProcurePilotDbContext>();
            tenantId = await context.Tenants.IgnoreQueryFilters()
                .Where(value => value.Name == $"Golden path {runId}")
                .Select(value => value.Id)
                .SingleAsync(cancellationToken);
        }

        var vendorId = Guid.NewGuid();
        var purchaseOrderLineId = Guid.NewGuid();
        var matchingRuleId = Guid.NewGuid();
        {
            var tenantServices = new ServiceCollection();
            tenantServices.AddLogging();
            tenantServices.AddScoped<ITenantContext>(_ => new FixedTenantContext(tenantId));
            tenantServices.AddInfrastructure(database!, blobStorage!);
            await using var tenantProvider = tenantServices.BuildServiceProvider();
            await using var tenantScope = tenantProvider.CreateAsyncScope();
            var context = tenantScope.ServiceProvider.GetRequiredService<ProcurePilotDbContext>();
            context.Vendors.Add(new Vendor(vendorId, tenantId, $"GOLDEN-{runId}", "Golden Supplier", $"TAX-{runId}"));
            var order = new PurchaseOrder(
                Guid.NewGuid(), tenantId, vendorId, $"PO-GOLDEN-{runId}", new DateOnly(2026, 10, 18), "USD", "Open", 100m);
            order.AddLine(purchaseOrderLineId, 1, "Golden test item", 1m, 100m);
            context.PurchaseOrders.Add(order);
            context.MatchingRules.Add(new MatchingRule(
                matchingRuleId,
                tenantId,
                MatchingRuleType.PriceVariance,
                1m,
                DateTimeOffset.UtcNow.AddDays(-1)));
            await context.SaveChangesAsync(cancellationToken);
        }

        using var anonymousClient = app.CreateHttpClient("web", "http");
        using var protectedPage = await anonymousClient.GetAsync("/invoices/upload", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, protectedPage.StatusCode);
        Assert.Contains("Sign in", await protectedPage.Content.ReadAsStringAsync(cancellationToken),
            StringComparison.OrdinalIgnoreCase);

        using var client = app.CreateHttpClient("web", "http");
        var loginHtml = await client.GetStringAsync("/account/login", cancellationToken);
        var token = Regex.Match(loginHtml,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);
        Assert.True(token.Success, "Login must include an antiforgery token.");

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["ReturnUrl"] = "/invoices/upload",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        });
        using var loginResponse = await client.PostAsync("/account/login", form, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var uploadHtml = await loginResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Upload an invoice", uploadHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Invalid email or password", uploadHtml, StringComparison.Ordinal);

        using var playwright = await Playwright.CreateAsync();
        var browserOptions = new BrowserTypeLaunchOptions { Headless = true };
        var browserChannel = Environment.GetEnvironmentVariable("PLAYWRIGHT_CHANNEL");
        if (!string.IsNullOrWhiteSpace(browserChannel))
        {
            browserOptions.Channel = browserChannel;
        }

        await using var browser = await playwright.Chromium.LaunchAsync(browserOptions);
        var page = await browser.NewPageAsync();
        var browserErrors = new List<string>();
        page.PageError += (_, error) => browserErrors.Add(error);
        page.Console += (_, message) => browserErrors.Add($"{message.Type}: {message.Text}");
        var baseAddress = client.BaseAddress ?? throw new InvalidOperationException("The AppHost web endpoint is missing.");
        await page.GotoAsync(new Uri(baseAddress, "/account/login?ReturnUrl=%2Finvoices%2Fupload").ToString());
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Upload an invoice" }).WaitForAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.Locator("#invoice-file").SetInputFilesAsync(new FilePayload
        {
            Name = "golden-invoice.pdf",
            MimeType = "application/pdf",
            Buffer = "%PDF-1.7\nGolden path invoice fixture\n%%EOF"u8.ToArray()
        });
        try
        {
            await page.GetByText("golden-invoice.pdf", new() { Exact = true }).WaitForAsync(new() { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            var selectedFile = await page.Locator("#invoice-file").EvaluateAsync<string>("element => element.files?.[0]?.name ?? 'none'");
            var bodyText = await page.Locator("body").InnerTextAsync();
            throw new InvalidOperationException($"Blazor did not reflect the selected file. Input file: {selectedFile}. Browser errors: {string.Join(" | ", browserErrors)}. Page: {bodyText}");
        }
        await page.GetByRole(AriaRole.Button, new() { Name = "Upload and start processing" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Review extracted data" }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Review extracted data" }).WaitForAsync();
        await page.GetByLabel("Invoice number", new() { Exact = true }).FillAsync($"INV-GOLDEN-{runId}");
        await page.GetByLabel("Vendor", new() { Exact = true }).SelectOptionAsync(vendorId.ToString());
        await page.GetByLabel("Purchase order", new() { Exact = true }).FillAsync($"PO-GOLDEN-{runId}");
        await page.GetByLabel("Invoice date", new() { Exact = true }).FillAsync("2026-10-18");
        await page.GetByLabel("Currency", new() { Exact = true }).FillAsync("USD");
        await page.GetByLabel("Total amount", new() { Exact = true }).FillAsync("102");
        await page.GetByLabel("Description", new() { Exact = true }).FillAsync("Golden test item");
        await page.GetByLabel("Quantity").FillAsync("1");
        await page.GetByLabel("Unit price").FillAsync("102");
        await page.GetByLabel("I checked every flagged critical field against the source document.").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirm reviewed data" }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Match invoice lines" }).WaitForAsync();
        if (await page.Locator("#match-invoice-line").CountAsync() == 0)
        {
            throw new InvalidOperationException($"Matching did not become available after review: {await page.Locator("body").InnerTextAsync()}");
        }
        await page.Locator("#match-invoice-line").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.Locator("#match-po-line").SelectOptionAsync(purchaseOrderLineId.ToString());
        await page.Locator("#match-rule").SelectOptionAsync(matchingRuleId.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Evaluate price variance" }).ClickAsync();
        var exceptionLink = page.GetByRole(AriaRole.Link, new() { Name = "Open exception workspace →" });
        try
        {
            await exceptionLink.WaitForAsync(new() { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException($"Matching did not create an exception link. Browser errors: {string.Join(" | ", browserErrors)}. Page: {await page.Locator("body").InnerTextAsync()}");
        }
        var exceptionPath = await exceptionLink.GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(exceptionPath));
        var exceptionId = Guid.Parse(exceptionPath![(exceptionPath.LastIndexOf('/') + 1)..]);

        await page.GotoAsync(new Uri(baseAddress, "/work-queue").ToString());
        var caseReference = $"EXC-{exceptionId:N}"[..12].ToUpperInvariant();
        await page.GetByRole(AriaRole.Link, new() { Name = caseReference }).ClickAsync();
        try
        {
            await page.GetByRole(AriaRole.Heading, new() { Name = "Factual evidence" }).WaitForAsync(new() { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException($"The queue item did not open the exception workspace. URL: {page.Url}. Browser errors: {string.Join(" | ", browserErrors)}. Page: {await page.Locator("body").InnerTextAsync()}");
        }
        await page.GetByRole(AriaRole.Button, new() { Name = "Generate explanation" }).ClickAsync();
        try
        {
            await page.GetByText("Explanation unavailable").WaitForAsync(new() { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException($"AI analysis did not render its unavailable state. Browser errors: {string.Join(" | ", browserErrors)}. Page: {await page.Locator("body").InnerTextAsync()}");
        }
        await page.GetByLabel("Override reason").WaitForAsync();
        const string overrideReason = "Verified invoice and purchase order evidence; AP manager accepted the documented variance.";
        await page.GetByLabel("Override reason").FillAsync(overrideReason);
        await page.GetByLabel("Override reason").PressAsync("Tab");
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirm override" }).ClickAsync();
        try
        {
            await page.GetByText("Override recorded. The exception is now resolved and auditable.").WaitForAsync(new() { Timeout = 10000 });
            await page.GetByText(overrideReason, new() { Exact = true }).WaitForAsync(new() { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException($"The browser override or audit trail did not complete. URL: {page.Url}. Browser errors: {string.Join(" | ", browserErrors)}. Page: {await page.Locator("body").InnerTextAsync()}");
        }

        var readServices = new ServiceCollection();
        readServices.AddLogging();
        readServices.AddScoped<ITenantContext>(_ => new FixedTenantContext(tenantId));
        readServices.AddInfrastructure(database!, blobStorage!);
        await using var readProvider = readServices.BuildServiceProvider();
        await using var readScope = readProvider.CreateAsyncScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<ProcurePilotDbContext>();
        var persistedCase = await readContext.ExceptionCases.SingleAsync(value => value.Id == exceptionId, cancellationToken);
        Assert.Equal(ExceptionCaseStatus.Resolved, persistedCase.Status);
        var resolution = await readContext.Resolutions.SingleAsync(value => value.ExceptionCaseId == exceptionId, cancellationToken);
        Assert.Equal(overrideReason, resolution.Reason);
        var auditEvent = await readContext.AuditEvents.SingleAsync(value => value.ExceptionCaseId == exceptionId, cancellationToken);
        Assert.Equal(overrideReason, auditEvent.Reason);
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public Guid? TenantId => null;

        public Guid RequireTenantId() =>
            throw new InvalidOperationException("A tenant is required for this test.");
    }

    private sealed class FixedTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid? TenantId { get; } = tenantId;
        public Guid RequireTenantId() => tenantId;
    }

}
