using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProcurePilot.Application.Purchasing;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Infrastructure.Identity;
using ProcurePilot.Infrastructure.Persistence;
using ProcurePilot.Infrastructure.Persistence.Queries;
using ProcurePilot.Application.Invoices.Upload;
using ProcurePilot.Application.Invoices.Review;
using ProcurePilot.Application.Dashboard;
using ProcurePilot.Infrastructure.Invoices;
using ProcurePilot.Infrastructure.Storage;

namespace ProcurePilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string invoiceBlobConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceBlobConnectionString);

        services.TryAddScoped<ITenantContext, NullTenantContext>();

        services.AddDbContext<ProcurePilotDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ProcurePilotDbContext).Assembly.FullName)));

        services.AddScoped<IPurchaseOrderQueries, PurchaseOrderQueries>();
        services.AddScoped<IWorkspaceDashboardQueries, WorkspaceDashboardQueries>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new BlobServiceClient(invoiceBlobConnectionString));
        services.AddScoped<IInvoiceDocumentStorage, AzureBlobInvoiceDocumentStorage>();
        services.AddScoped<IInvoiceUploadService, InvoiceUploadService>();
        services.AddScoped<IInvoiceExtractionReviewService, InvoiceExtractionReviewService>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ProcurePilotDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<TenantClaimsPrincipalFactory>();

        return services;
    }
}
