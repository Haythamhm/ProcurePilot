using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Tenancy;
using ProcurePilot.Domain.Vendors;
using ProcurePilot.Infrastructure;
using ProcurePilot.Infrastructure.Identity;
using ProcurePilot.Infrastructure.Persistence;
using ProcurePilot.Web.DemoData;
using ProcurePilot.Web.Identity;

namespace ProcurePilot.IntegrationTests.Tests;

public sealed class AppHostTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    [Fact]
    public async Task IdentityVendorMigrationsTenantIsolationAndLoginWorkAgainstPostgreSql()
    {
        const string bootstrapEmail = "admin@integration.local";
        const string bootstrapPassword = "ProcurePilot!42";

        using var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ProcurePilot_AppHost>(cancellationToken);

        appHost.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        await using var app = await appHost.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("web", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var connectionString = await app.GetConnectionStringAsync("procurepilot", cancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        await SeedDemoDataAsync(
            connectionString,
            bootstrapEmail,
            bootstrapPassword,
            cancellationToken);

        var options = new DbContextOptionsBuilder<ProcurePilotDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new ProcurePilotDbContext(options, new TestTenantContext(null));
        Assert.True(await dbContext.Database.CanConnectAsync(cancellationToken));

        var appliedMigrations = await dbContext.Database
            .GetAppliedMigrationsAsync(cancellationToken);
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_InitialTechnicalBaseline", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddIdentityAndTenancy", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddVendorMasterData", StringComparison.Ordinal));

        await dbContext.Database.MigrateAsync(cancellationToken);
        var pendingMigrations = await dbContext.Database
            .GetPendingMigrationsAsync(cancellationToken);
        Assert.Empty(pendingMigrations);

        var bootstrapTenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleAsync(tenant => tenant.Name == "Integration Tenant", cancellationToken);
        var otherTenant = new Tenant(Guid.NewGuid(), $"Other Tenant {Guid.NewGuid():N}");
        dbContext.Tenants.Add(otherTenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        await using var bootstrapTenantContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(bootstrapTenant.Id));
        var visibleTenantIds = await bootstrapTenantContext.Tenants
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken);
        Assert.Equal([bootstrapTenant.Id], visibleTenantIds);

        var atlas = await bootstrapTenantContext.Vendors.SingleAsync(cancellationToken);
        Assert.Equal(VendorBootstrapper.AtlasExternalReference, atlas.ExternalReference);
        Assert.Equal(VendorBootstrapper.AtlasName, atlas.Name);
        Assert.Equal(VendorBootstrapper.AtlasTaxIdentifier, atlas.TaxIdentifier);
        Assert.Equal(bootstrapTenant.Id, atlas.TenantId);

        await using var otherTenantContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(otherTenant.Id));
        Assert.Empty(await otherTenantContext.Vendors.ToListAsync(cancellationToken));

        otherTenantContext.Vendors.Add(new Vendor(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            "CROSS-TENANT",
            "Cross Tenant Vendor"));
        var crossTenantWrite = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            otherTenantContext.SaveChangesAsync(cancellationToken));
        Assert.Equal("Cross-tenant writes are not allowed.", crossTenantWrite.Message);

        await using var missingTenantContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(null));
        Assert.Empty(await missingTenantContext.Tenants.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.Vendors.ToListAsync(cancellationToken));

        using var invalidClient = app.CreateHttpClient("web", "http");
        var invalidLoginPage = await invalidClient.GetAsync("/account/login", cancellationToken);
        var invalidLoginHtml = await invalidLoginPage.Content.ReadAsStringAsync(cancellationToken);
        using var invalidLoginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = bootstrapEmail,
            ["Input.Password"] = "WrongPassword!42",
            ["Input.RememberMe"] = "false",
            ["ReturnUrl"] = "/",
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(invalidLoginHtml)
        });
        var invalidLoginResponse = await invalidClient.PostAsync(
            "/account/login",
            invalidLoginContent,
            cancellationToken);
        var invalidLoginResult = await invalidLoginResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Invalid email or password.", invalidLoginResult, StringComparison.Ordinal);

        using var client = app.CreateHttpClient("web", "http");
        var loginPage = await client.GetAsync("/account/login", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);
        var loginHtml = await loginPage.Content.ReadAsStringAsync(cancellationToken);
        var antiforgeryToken = ExtractAntiforgeryToken(loginHtml);

        using var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = bootstrapEmail,
            ["Input.Password"] = bootstrapPassword,
            ["Input.RememberMe"] = "false",
            ["ReturnUrl"] = "/",
            ["__RequestVerificationToken"] = antiforgeryToken
        });

        var loginResponse = await client.PostAsync("/account/login", loginContent, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var homeHtml = await loginResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains(bootstrapEmail, homeHtml, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, "The login page did not render an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task SeedDemoDataAsync(
        string connectionString,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddScoped<ITenantContext>(_ => new TestTenantContext(null));
        services.AddInfrastructure(connectionString);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var bootstrapOptions = Options.Create(new IdentityBootstrapOptions
        {
            TenantName = "Integration Tenant",
            Email = email,
            Password = password,
            DisplayName = "Integration Admin"
        });
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurePilotDbContext>();
        var bootstrapper = new IdentityBootstrapper(
            bootstrapOptions,
            dbContext,
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>());

        await bootstrapper.SeedAsync(cancellationToken);

        var vendorBootstrapper = new VendorBootstrapper(
            bootstrapOptions,
            dbContext,
            scope.ServiceProvider.GetRequiredService<DbContextOptions<ProcurePilotDbContext>>());
        await vendorBootstrapper.SeedAsync(cancellationToken);
        await vendorBootstrapper.SeedAsync(cancellationToken);
    }

    private sealed class TestTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? TenantId { get; } = tenantId;

        public Guid RequireTenantId() => TenantId
            ?? throw new InvalidOperationException("A tenant is required for this test.");
    }
}
