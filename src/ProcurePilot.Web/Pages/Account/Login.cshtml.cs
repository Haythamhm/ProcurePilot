using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Domain.Tenancy;
using ProcurePilot.Infrastructure.Identity;
using ProcurePilot.Infrastructure.Persistence;

namespace ProcurePilot.Web.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ProcurePilotDbContext dbContext) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true
            ? LocalRedirect(GetSafeReturnUrl())
            : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email.Trim());
        if (user is not null)
        {
            var passwordResult = await signInManager.CheckPasswordSignInAsync(
                user,
                Input.Password,
                lockoutOnFailure: true);
            var tenantIsActive = passwordResult.Succeeded && await dbContext.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(tenant => tenant.Id == user.TenantId && tenant.Status == TenantStatus.Active);

            if (tenantIsActive)
            {
                await signInManager.SignInAsync(user, Input.RememberMe);
                return LocalRedirect(GetSafeReturnUrl());
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }

    private string GetSafeReturnUrl() =>
        !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? ReturnUrl
            : "/";

    public sealed class LoginInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
