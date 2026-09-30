using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Infrastructure;
using ProcurePilot.Infrastructure.Persistence;
using ProcurePilot.Web.Components;
using ProcurePilot.Web.DemoData;
using ProcurePilot.Web.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, ClaimsTenantContext>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("procurepilot")
        ?? throw new InvalidOperationException("Connection string 'procurepilot' is required."));
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(
        AuthorizationPolicies.ActiveTenant,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveTenantRequirement()));
builder.Services.AddScoped<IAuthorizationHandler, ActiveTenantAuthorizationHandler>();
builder.Services.Configure<IdentityBootstrapOptions>(
    builder.Configuration.GetSection(IdentityBootstrapOptions.SectionName));
builder.Services.AddScoped<IdentityBootstrapper>();
builder.Services.AddScoped<VendorBootstrapper>();
builder.Services.AddScoped<PurchaseOrderBootstrapper>();

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProcurePilotDbContext>();
    await dbContext.Database.MigrateAsync();
    var identityBootstrapper = scope.ServiceProvider.GetRequiredService<IdentityBootstrapper>();
    await identityBootstrapper.SeedAsync();
    var vendorBootstrapper = scope.ServiceProvider.GetRequiredService<VendorBootstrapper>();
    await vendorBootstrapper.SeedAsync();
    var purchaseOrderBootstrapper = scope.ServiceProvider
        .GetRequiredService<PurchaseOrderBootstrapper>();
    await purchaseOrderBootstrapper.SeedAsync();
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
