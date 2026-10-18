using System.Net;
using System.Text.RegularExpressions;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Infrastructure;
using ProcurePilot.Infrastructure.Identity;
using ProcurePilot.Infrastructure.Persistence;
using ProcurePilot.Web.Identity;

namespace ProcurePilot.E2ETests;

public sealed class GoldenPathEntryTests
{
    [Fact]
    public async Task UserSignsInAndReachesProtectedInvoiceUpload()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
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
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public Guid? TenantId => null;

        public Guid RequireTenantId() =>
            throw new InvalidOperationException("A tenant is required for this test.");
    }
}
