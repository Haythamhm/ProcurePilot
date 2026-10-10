using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Application.Invoices.Upload;
using ProcurePilot.Application.Invoices.Extraction;
using ProcurePilot.Application.Invoices.Review;
using ProcurePilot.Application.Matching;
using ProcurePilot.Domain.Exceptions;
using ProcurePilot.Domain.Invoices;
using ProcurePilot.Domain.Matching;
using ProcurePilot.Domain.Purchasing;
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
    public async Task IdentityMasterDataMigrationsTenantIsolationAndLoginWorkAgainstPostgreSql()
    {
        var runId = Guid.NewGuid().ToString("N");
        var bootstrapTenantName = $"Integration Tenant {runId}";
        var bootstrapEmail = $"admin-{runId}@integration.local";
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
        var invoiceBlobConnectionString = await app.GetConnectionStringAsync("invoiceblobs", cancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(invoiceBlobConnectionString));

        await SeedDemoDataAsync(
            connectionString,
            invoiceBlobConnectionString,
            bootstrapTenantName,
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
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddPurchaseOrderMasterData", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddInvoiceDomain", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddInvoiceDocumentUpload", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddInvoiceExtractionReview", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddMatchingRule", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddPriceVarianceMatchResult", StringComparison.Ordinal));
        Assert.Contains(
            appliedMigrations,
            migration => migration.EndsWith("_AddExceptionCaseStateMachine", StringComparison.Ordinal));

        await dbContext.Database.MigrateAsync(cancellationToken);
        var pendingMigrations = await dbContext.Database
            .GetPendingMigrationsAsync(cancellationToken);
        Assert.Empty(pendingMigrations);

        var bootstrapTenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleAsync(tenant => tenant.Name == bootstrapTenantName, cancellationToken);
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

        var demoPurchaseOrder = await bootstrapTenantContext.PurchaseOrders
            .Include(purchaseOrder => purchaseOrder.Lines)
            .SingleAsync(cancellationToken);
        Assert.Equal(PurchaseOrderBootstrapper.DemoNumber, demoPurchaseOrder.Number);
        Assert.Equal(atlas.Id, demoPurchaseOrder.VendorId);
        Assert.Equal(PurchaseOrderBootstrapper.DemoOrderDate, demoPurchaseOrder.OrderDate);
        Assert.Equal(PurchaseOrderBootstrapper.DemoCurrency, demoPurchaseOrder.Currency);
        Assert.Equal(PurchaseOrderBootstrapper.DemoStatus, demoPurchaseOrder.Status);
        Assert.Equal(PurchaseOrderBootstrapper.DemoTotalAmount, demoPurchaseOrder.TotalAmount);
        var demoLine = Assert.Single(demoPurchaseOrder.Lines);
        Assert.Equal(PurchaseOrderBootstrapper.DemoLineDescription, demoLine.Description);
        Assert.Equal(PurchaseOrderBootstrapper.DemoLineQuantity, demoLine.Quantity);
        Assert.Equal(PurchaseOrderBootstrapper.DemoLineUnitPrice, demoLine.UnitPrice);

        var initialMatchingRule = new MatchingRule(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            MatchingRuleType.PriceVariance,
            3m,
            new DateTimeOffset(2026, 10, 6, 8, 0, 0, TimeSpan.Zero));
        bootstrapTenantContext.MatchingRules.Add(initialMatchingRule);
        await bootstrapTenantContext.SaveChangesAsync(cancellationToken);

        var currentMatchingRule = initialMatchingRule.CreateNextVersion(
            Guid.NewGuid(),
            2.5m,
            initialMatchingRule.EffectiveFromUtc.AddDays(30));
        bootstrapTenantContext.MatchingRules.Add(currentMatchingRule);
        await bootstrapTenantContext.SaveChangesAsync(cancellationToken);

        var bootstrapInvoice = new Invoice(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            atlas.Id,
            $"INV-INTEGRATION-{Guid.NewGuid():N}",
            new DateOnly(2026, 10, 5),
            "USD",
            131.25m);
        var bootstrapInvoiceLine = bootstrapInvoice.AddLine(
            Guid.NewGuid(),
            1,
            "Persisted invoice line",
            1m,
            131.25m);
        bootstrapTenantContext.Invoices.Add(bootstrapInvoice);
        var withinToleranceInvoice = new Invoice(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            atlas.Id,
            $"INV-WITHIN-TOLERANCE-{Guid.NewGuid():N}",
            new DateOnly(2026, 10, 5),
            "USD",
            127.50m);
        var withinToleranceInvoiceLine = withinToleranceInvoice.AddLine(
            Guid.NewGuid(),
            1,
            "Within tolerance invoice line",
            1m,
            127.50m);
        bootstrapTenantContext.Invoices.Add(withinToleranceInvoice);
        await bootstrapTenantContext.SaveChangesAsync(cancellationToken);

        var matchingServices = new ServiceCollection();
        matchingServices.AddLogging();
        matchingServices.AddDataProtection();
        matchingServices.AddScoped<ITenantContext>(_ => new TestTenantContext(bootstrapTenant.Id));
        matchingServices.AddInfrastructure(connectionString, invoiceBlobConnectionString);
        matchingServices.AddSingleton<TimeProvider>(new TestTimeProvider(
            initialMatchingRule.EffectiveFromUtc.AddMinutes(1)));

        PriceVarianceMatchOutcome failedOutcome;
        PriceVarianceMatchOutcome passedOutcome;
        await using (var matchingProvider = matchingServices.BuildServiceProvider())
        await using (var matchingScope = matchingProvider.CreateAsyncScope())
        {
            var matchingService = matchingScope.ServiceProvider
                .GetRequiredService<IPriceVarianceMatchingService>();
            failedOutcome = await matchingService.EvaluateAsync(
                new PriceVarianceMatchCommand(
                    bootstrapInvoiceLine.Id,
                    demoLine.Id,
                    initialMatchingRule.Id),
                cancellationToken);
            passedOutcome = await matchingService.EvaluateAsync(
                new PriceVarianceMatchCommand(
                    withinToleranceInvoiceLine.Id,
                    demoLine.Id,
                    initialMatchingRule.Id),
                cancellationToken);
        }

        Assert.Equal(MatchResultStatus.Failed, failedOutcome.Status);
        Assert.Equal(MatchExceptionCode.PriceVariance, failedOutcome.ExceptionCode);
        Assert.NotNull(failedOutcome.ExceptionCaseId);
        Assert.Equal(MatchResultStatus.Passed, passedOutcome.Status);
        Assert.Null(passedOutcome.ExceptionCode);
        Assert.Null(passedOutcome.ExceptionCaseId);

        var crossTenantMatchingServices = new ServiceCollection();
        crossTenantMatchingServices.AddLogging();
        crossTenantMatchingServices.AddDataProtection();
        crossTenantMatchingServices.AddScoped<ITenantContext>(_ => new TestTenantContext(otherTenant.Id));
        crossTenantMatchingServices.AddInfrastructure(connectionString, invoiceBlobConnectionString);
        crossTenantMatchingServices.AddSingleton<TimeProvider>(new TestTimeProvider(
            initialMatchingRule.EffectiveFromUtc.AddMinutes(1)));
        await using (var crossTenantProvider = crossTenantMatchingServices.BuildServiceProvider())
        await using (var crossTenantScope = crossTenantProvider.CreateAsyncScope())
        {
            var crossTenantMatchingService = crossTenantScope.ServiceProvider
                .GetRequiredService<IPriceVarianceMatchingService>();
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                crossTenantMatchingService.EvaluateAsync(
                    new PriceVarianceMatchCommand(
                        bootstrapInvoiceLine.Id,
                        demoLine.Id,
                        initialMatchingRule.Id),
                    cancellationToken));
        }

        await using var invoiceReadContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(bootstrapTenant.Id));
        var persistedInvoice = await invoiceReadContext.Invoices
            .Include(invoice => invoice.Lines)
            .SingleAsync(invoice => invoice.Id == bootstrapInvoice.Id, cancellationToken);
        Assert.Equal(InvoiceProcessingStatus.Processing, persistedInvoice.ProcessingStatus);
        Assert.Equal("Persisted invoice line", Assert.Single(persistedInvoice.Lines).Description);

        var persistedMatchingRules = await invoiceReadContext.MatchingRules
            .OrderBy(rule => rule.Version)
            .ToListAsync(cancellationToken);
        Assert.Collection(
            persistedMatchingRules,
            first =>
            {
                Assert.Equal(1, first.Version);
                Assert.Equal(3m, first.AllowedTolerancePercent);
                Assert.False(first.IsCurrent);
            },
            second =>
            {
                Assert.Equal(2, second.Version);
                Assert.Equal(2.5m, second.AllowedTolerancePercent);
                Assert.True(second.IsCurrent);
            });

        var persistedMatchResult = await invoiceReadContext.MatchResults
            .SingleAsync(
                result => result.Id == failedOutcome.MatchResultId,
                cancellationToken);
        Assert.Equal(MatchResultStatus.Failed, persistedMatchResult.Status);
        Assert.Equal(MatchExceptionCode.PriceVariance, persistedMatchResult.ExceptionCode);
        Assert.Equal(5m, persistedMatchResult.VariancePercent);
        Assert.Equal(3m, persistedMatchResult.AllowedTolerancePercent);
        Assert.Equal(1, persistedMatchResult.MatchingRuleVersion);
        Assert.Equal(initialMatchingRule.Id, persistedMatchResult.MatchingRuleId);

        var persistedExceptionCase = await invoiceReadContext.ExceptionCases
            .SingleAsync(cancellationToken);
        Assert.Equal(bootstrapTenant.Id, persistedExceptionCase.TenantId);
        Assert.Equal(bootstrapInvoice.Id, persistedExceptionCase.InvoiceId);
        Assert.Equal(demoPurchaseOrder.Id, persistedExceptionCase.PurchaseOrderId);
        Assert.Equal(failedOutcome.MatchResultId, persistedExceptionCase.MatchResultId);
        Assert.Equal(MatchExceptionCode.PriceVariance, persistedExceptionCase.Type);
        Assert.Equal(ExceptionCaseStatus.New, persistedExceptionCase.Status);
        Assert.Null(persistedExceptionCase.ResolvedAtUtc);
        Assert.Equal(2, await invoiceReadContext.MatchResults.CountAsync(cancellationToken));
        Assert.Equal(1, await invoiceReadContext.ExceptionCases.CountAsync(
            value => value.MatchResultId == failedOutcome.MatchResultId,
            cancellationToken));
        Assert.False(await invoiceReadContext.ExceptionCases.AnyAsync(
            value => value.MatchResultId == passedOutcome.MatchResultId,
            cancellationToken));

        var bootstrapUser = await invoiceReadContext.Users
            .SingleAsync(user => user.Email == bootstrapEmail, cancellationToken);
        var uploadServices = new ServiceCollection();
        uploadServices.AddLogging();
        uploadServices.AddDataProtection();
        uploadServices.AddScoped<ITenantContext>(_ => new TestTenantContext(bootstrapTenant.Id));
        uploadServices.Configure<InvoiceExtractionReviewOptions>(options =>
        {
            options.ConfidenceThreshold = 0.8;
            options.CriticalFields =
            [
                "InvoiceNumber",
                "SupplierName",
                "PurchaseOrderNumber",
                "InvoiceDate",
                "Currency",
                "TotalAmount"
            ];
        });
        uploadServices.AddInfrastructure(connectionString, invoiceBlobConnectionString);

        Guid uploadedInvoiceId;
        await using (var uploadProvider = uploadServices.BuildServiceProvider())
        await using (var uploadScope = uploadProvider.CreateAsyncScope())
        {
            var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\n% ProcurePilot integration invoice");
            await using var pdfStream = new MemoryStream(pdfBytes);
            var upload = await uploadScope.ServiceProvider
                .GetRequiredService<IInvoiceUploadService>()
                .UploadAsync(
                    new InvoiceUploadRequest(pdfStream, "supplier-invoice.pdf", "application/pdf", pdfBytes.Length),
                    bootstrapUser.Id,
                    cancellationToken);
            uploadedInvoiceId = upload.InvoiceId;

            var reviewService = uploadScope.ServiceProvider
                .GetRequiredService<IInvoiceExtractionReviewService>();
            await reviewService.SaveExtractionAsync(
                upload.InvoiceId,
                CreateLowConfidenceExtraction(),
                cancellationToken);

            var unconfirmedCommand = new ConfirmInvoiceExtractionCommand(
                upload.InvoiceId,
                atlas.Id,
                "INV-REVIEWED-42",
                PurchaseOrderBootstrapper.DemoNumber,
                new DateOnly(2026, 10, 6),
                "USD",
                100m,
                20m,
                120m,
                [new ConfirmInvoiceLineCommand(1, "Control module", 1m, 100m)],
                ConfirmLowConfidenceFields: false);
            await Assert.ThrowsAsync<InvoiceReviewValidationException>(() =>
                reviewService.ConfirmAsync(unconfirmedCommand, bootstrapUser.Id, cancellationToken));

            await reviewService.ConfirmAsync(
                unconfirmedCommand with { ConfirmLowConfidenceFields = true },
                bootstrapUser.Id,
                cancellationToken);

            await using var uploadReadContext = new ProcurePilotDbContext(
                options,
                new TestTenantContext(bootstrapTenant.Id));
            var uploadedInvoice = await uploadReadContext.Invoices
                .SingleAsync(invoice => invoice.Id == upload.InvoiceId, cancellationToken);
            var uploadedDocument = await uploadReadContext.InvoiceDocuments
                .SingleAsync(document => document.Id == upload.DocumentId, cancellationToken);

            Assert.Equal(InvoiceProcessingStatus.Ready, uploadedInvoice.ProcessingStatus);
            Assert.Equal("INV-REVIEWED-42", uploadedInvoice.InvoiceNumber);
            Assert.Equal(bootstrapUser.Id, uploadedDocument.UploadedByUserId);
            Assert.Equal("supplier-invoice.pdf", uploadedDocument.OriginalFileName);
            Assert.StartsWith($"{bootstrapTenant.Id:N}/{upload.InvoiceId:N}/", uploadedDocument.StorageReference);

            var blob = new BlobServiceClient(invoiceBlobConnectionString)
                .GetBlobContainerClient("invoice-documents")
                .GetBlobClient(uploadedDocument.StorageReference);
            Assert.True(await blob.ExistsAsync(cancellationToken));

            var extractionSnapshot = await uploadReadContext.InvoiceExtractionSnapshots
                .SingleAsync(snapshot => snapshot.InvoiceId == upload.InvoiceId, cancellationToken);
            Assert.Equal(bootstrapUser.Id, extractionSnapshot.ReviewedByUserId);
            Assert.NotNull(extractionSnapshot.ReviewedAtUtc);
            Assert.NotNull(extractionSnapshot.ReviewedPayload);
        }

        await using var otherTenantContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(otherTenant.Id));
        Assert.Empty(await otherTenantContext.Vendors.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.PurchaseOrders.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.PurchaseOrderLines.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.Invoices.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.InvoiceLines.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.InvoiceDocuments.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.InvoiceExtractionSnapshots.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.MatchingRules.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.MatchResults.ToListAsync(cancellationToken));
        Assert.Empty(await otherTenantContext.ExceptionCases.ToListAsync(cancellationToken));
        Assert.DoesNotContain(
            uploadedInvoiceId,
            await otherTenantContext.InvoiceExtractionSnapshots
                .Select(snapshot => snapshot.InvoiceId)
                .ToListAsync(cancellationToken));

        var otherTenantVendor = new Vendor(
            Guid.NewGuid(),
            otherTenant.Id,
            "OTHER-VENDOR",
            "Other Tenant Vendor");
        var otherTenantPurchaseOrder = new PurchaseOrder(
            Guid.NewGuid(),
            otherTenant.Id,
            otherTenantVendor.Id,
            "PO-OTHER-TENANT",
            new DateOnly(2026, 1, 17),
            "USD",
            "Open",
            25m);
        var otherTenantPurchaseOrderLine = otherTenantPurchaseOrder.AddLine(
            Guid.NewGuid(),
            1,
            "Tenant secret line",
            1m,
            25m);
        var otherTenantInvoice = new Invoice(
            Guid.NewGuid(),
            otherTenant.Id,
            otherTenantVendor.Id,
            "INV-OTHER-TENANT",
            new DateOnly(2026, 10, 5),
            "USD",
            25m);
        var otherTenantInvoiceLine = otherTenantInvoice.AddLine(
            Guid.NewGuid(),
            1,
            "Tenant secret invoice line",
            1m,
            25m);
        var otherTenantRule = new MatchingRule(
            Guid.NewGuid(),
            otherTenant.Id,
            MatchingRuleType.PriceVariance,
            4m,
            DateTimeOffset.UtcNow.AddMinutes(-1));
        otherTenantContext.Vendors.Add(otherTenantVendor);
        otherTenantContext.PurchaseOrders.Add(otherTenantPurchaseOrder);
        otherTenantContext.Invoices.Add(otherTenantInvoice);
        otherTenantContext.MatchingRules.Add(otherTenantRule);
        await otherTenantContext.SaveChangesAsync(cancellationToken);

        otherTenantContext.MatchResults.Add(MatchResult.EvaluatePriceVariance(
            Guid.NewGuid(),
            otherTenantInvoiceLine,
            otherTenantPurchaseOrderLine,
            otherTenantRule,
            DateTimeOffset.UtcNow));
        await otherTenantContext.SaveChangesAsync(cancellationToken);

        Assert.DoesNotContain(
            otherTenantInvoice.Id,
            await invoiceReadContext.Invoices.Select(invoice => invoice.Id).ToListAsync(cancellationToken));
        Assert.DoesNotContain(
            otherTenantInvoiceLine.Id,
            await invoiceReadContext.MatchResults
                .Select(result => result.InvoiceLineId)
                .ToListAsync(cancellationToken));

        otherTenantContext.Vendors.Add(new Vendor(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            "CROSS-TENANT",
            "Cross Tenant Vendor"));
        var crossTenantWrite = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            otherTenantContext.SaveChangesAsync(cancellationToken));
        Assert.Equal("Cross-tenant writes are not allowed.", crossTenantWrite.Message);

        await using var crossTenantPurchaseOrderContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(otherTenant.Id));
        crossTenantPurchaseOrderContext.PurchaseOrders.Add(new PurchaseOrder(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            atlas.Id,
            "CROSS-TENANT-PO",
            new DateOnly(2026, 1, 16),
            "USD",
            "Open",
            10m));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            crossTenantPurchaseOrderContext.SaveChangesAsync(cancellationToken));

        await using var crossTenantInvoiceContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(otherTenant.Id));
        crossTenantInvoiceContext.Invoices.Add(new Invoice(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            atlas.Id,
            "CROSS-TENANT-INVOICE",
            new DateOnly(2026, 10, 5),
            "USD",
            10m));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            crossTenantInvoiceContext.SaveChangesAsync(cancellationToken));

        await using var crossTenantMatchingRuleContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(otherTenant.Id));
        crossTenantMatchingRuleContext.MatchingRules.Add(new MatchingRule(
            Guid.NewGuid(),
            bootstrapTenant.Id,
            MatchingRuleType.PriceVariance,
            3m,
            DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            crossTenantMatchingRuleContext.SaveChangesAsync(cancellationToken));

        await using var missingTenantContext = new ProcurePilotDbContext(
            options,
            new TestTenantContext(null));
        Assert.Empty(await missingTenantContext.Tenants.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.Vendors.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.PurchaseOrders.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.PurchaseOrderLines.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.Invoices.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.InvoiceLines.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.MatchingRules.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.MatchResults.ToListAsync(cancellationToken));
        Assert.Empty(await missingTenantContext.ExceptionCases.ToListAsync(cancellationToken));

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

        var purchaseOrdersResponse = await client.GetAsync("/purchase-orders", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, purchaseOrdersResponse.StatusCode);
        var purchaseOrdersHtml = await purchaseOrdersResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains(PurchaseOrderBootstrapper.DemoNumber, purchaseOrdersHtml, StringComparison.Ordinal);
        Assert.Contains(VendorBootstrapper.AtlasName, purchaseOrdersHtml, StringComparison.Ordinal);
        Assert.Contains(PurchaseOrderBootstrapper.DemoLineDescription, purchaseOrdersHtml, StringComparison.Ordinal);
        Assert.Contains("Purchase Orders", purchaseOrdersHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(otherTenantPurchaseOrder.Number, purchaseOrdersHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant secret line", purchaseOrdersHtml, StringComparison.Ordinal);

        var uploadPageResponse = await client.GetAsync("/invoices/upload", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, uploadPageResponse.StatusCode);
        var uploadPageHtml = await uploadPageResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Upload an invoice", uploadPageHtml, StringComparison.Ordinal);
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

    private static InvoiceExtractionResult CreateLowConfidenceExtraction() => new()
    {
        InvoiceNumber = Extracted<string?>("INV-AI-42", 0.62),
        SupplierName = Extracted<string?>(VendorBootstrapper.AtlasName, 0.98),
        PurchaseOrderNumber = Extracted<string?>(PurchaseOrderBootstrapper.DemoNumber, 0.97),
        InvoiceDate = Extracted<DateOnly?>(new DateOnly(2026, 10, 6), 0.96),
        Currency = Extracted<string?>("USD", 0.99),
        SubtotalAmount = Extracted<decimal?>(100m, 0.95),
        TaxAmount = Extracted<decimal?>(20m, 0.95),
        TotalAmount = Extracted<decimal?>(120m, 0.99),
        LineItems =
        [
            new InvoiceLineExtraction
            {
                LineNumber = 1,
                Description = Extracted<string?>("Control module", 0.94),
                Quantity = Extracted<decimal?>(1m, 0.98),
                UnitPrice = Extracted<decimal?>(100m, 0.97),
                LineTotal = Extracted<decimal?>(100m, 0.97)
            }
        ]
    };

    private static ExtractedField<T> Extracted<T>(T value, double confidence) => new()
    {
        Value = value,
        Confidence = confidence
    };

    private static async Task SeedDemoDataAsync(
        string connectionString,
        string invoiceBlobConnectionString,
        string tenantName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddScoped<ITenantContext>(_ => new TestTenantContext(null));
        services.AddInfrastructure(connectionString, invoiceBlobConnectionString);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var bootstrapOptions = Options.Create(new IdentityBootstrapOptions
        {
            TenantName = tenantName,
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

        var purchaseOrderBootstrapper = new PurchaseOrderBootstrapper(
            bootstrapOptions,
            dbContext,
            scope.ServiceProvider.GetRequiredService<DbContextOptions<ProcurePilotDbContext>>());
        await purchaseOrderBootstrapper.SeedAsync(cancellationToken);
        await purchaseOrderBootstrapper.SeedAsync(cancellationToken);
    }

    private sealed class TestTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? TenantId { get; } = tenantId;

        public Guid RequireTenantId() => TenantId
            ?? throw new InvalidOperationException("A tenant is required for this test.");
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
