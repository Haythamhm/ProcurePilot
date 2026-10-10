using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProcurePilot.UnitTests;

public sealed partial class FrontendDesignSystemTests
{
    private static readonly HashSet<string> ApprovedHexColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "#F1F6F9",
        "#394867",
        "#212A3E",
        "#9BA4B5"
    };

    [Fact]
    public void ApplicationStylesUseOnlyTheApprovedPalette()
    {
        var repositoryRoot = FindRepositoryRoot();
        var webRoot = Path.Combine(repositoryRoot, "src", "ProcurePilot.Web");
        var violations = new List<string>();

        foreach (var cssPath in Directory.EnumerateFiles(webRoot, "*.css", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(webRoot, cssPath);
            var firstSegment = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (firstSegment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || firstSegment.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || relativePath.StartsWith(
                    Path.Combine("wwwroot", "lib") + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var css = File.ReadAllText(cssPath);
            foreach (Match match in HexColorPattern().Matches(css))
            {
                if (!ApprovedHexColors.Contains(match.Value))
                {
                    violations.Add($"{Path.GetRelativePath(repositoryRoot, cssPath)}: {match.Value}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Custom frontend styles contain colors outside the approved palette:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void FontelloBundleContainsTheConfiguredNavigationGlyphs()
    {
        var repositoryRoot = FindRepositoryRoot();
        var fontelloRoot = Path.Combine(
            repositoryRoot,
            "src",
            "ProcurePilot.Web",
            "wwwroot",
            "assets",
            "fontello");
        var configPath = Path.Combine(fontelloRoot, "config.json");
        var fontPath = Path.Combine(fontelloRoot, "font", "procurepilot-icons.woff2");
        var licensePath = Path.Combine(fontelloRoot, "LICENSE.txt");

        Assert.True(File.Exists(fontPath), "The generated Fontello WOFF2 font is missing.");
        Assert.True(File.Exists(licensePath), "The generated Fontello license file is missing.");

        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var glyphs = config.RootElement
            .GetProperty("glyphs")
            .EnumerateArray()
            .Select(glyph => glyph.GetProperty("css").GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("home", glyphs);
        Assert.Contains("purchase-order", glyphs);
        Assert.Contains("logout", glyphs);
        Assert.Contains("menu", glyphs);
        Assert.Contains("user", glyphs);
    }

    [Fact]
    public void CriticalSystemStatesUseTheSharedAccessiblePattern()
    {
        var repositoryRoot = FindRepositoryRoot();
        var componentsRoot = Path.Combine(repositoryRoot, "src", "ProcurePilot.Web", "Components");

        var errorPage = File.ReadAllText(Path.Combine(componentsRoot, "Pages", "Error.razor"));
        var notFoundPage = File.ReadAllText(Path.Combine(componentsRoot, "Pages", "NotFound.razor"));
        var reconnectModal = File.ReadAllText(Path.Combine(componentsRoot, "Layout", "ReconnectModal.razor"));

        Assert.Contains("<SystemState", errorPage, StringComparison.Ordinal);
        Assert.Contains("<SystemState", notFoundPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Development Mode", errorPage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-labelledby=\"components-reconnect-title\"", reconnectModal, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"assertive\"", reconnectModal, StringComparison.Ordinal);
        Assert.Contains("type=\"button\"", reconnectModal, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceAndExtractionReviewUseRealApplicationServices()
    {
        var repositoryRoot = FindRepositoryRoot();
        var pagesRoot = Path.Combine(repositoryRoot, "src", "ProcurePilot.Web", "Components", "Pages");
        var home = File.ReadAllText(Path.Combine(pagesRoot, "Home.razor"));
        var review = File.ReadAllText(Path.Combine(pagesRoot, "InvoiceExtractionReview.razor"));

        Assert.Contains("IWorkspaceDashboardQueries", home, StringComparison.Ordinal);
        Assert.Contains("RecentInvoices", home, StringComparison.Ordinal);
        Assert.DoesNotContain("TODO", home, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IInvoiceExtractionReviewService", review, StringComparison.Ordinal);
        Assert.Contains("ConfirmLowConfidenceFields", review, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"step\"", review, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkQueueUsesTenantScopedQueryAndAccessibleTableStates()
    {
        var repositoryRoot = FindRepositoryRoot();
        var workQueue = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "ProcurePilot.Web",
            "Components",
            "Pages",
            "WorkQueue.razor"));
        var workQueueTable = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "ProcurePilot.Web",
            "Components",
            "Shared",
            "WorkQueueTable.razor"));

        Assert.Contains("IExceptionWorkQueueQueries", workQueue, StringComparison.Ordinal);
        Assert.Contains("AuthorizationPolicies.ActiveTenant", workQueue, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", workQueue, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Work queue views\"", workQueue, StringComparison.Ordinal);
        Assert.Contains("aria-current", workQueue, StringComparison.Ordinal);
        Assert.Contains("ExceptionWorkQueueFilter.AssignedToMe", workQueue, StringComparison.Ordinal);
        Assert.Contains("<caption class=\"visually-hidden\">", workQueueTable, StringComparison.Ordinal);
        Assert.Contains("scope=\"col\"", workQueueTable, StringComparison.Ordinal);
        Assert.Contains("scope=\"row\"", workQueueTable, StringComparison.Ordinal);
        Assert.Contains(">Age<", workQueueTable, StringComparison.Ordinal);
        Assert.Contains(">Priority<", workQueueTable, StringComparison.Ordinal);
        Assert.DoesNotContain("TODO", workQueue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExceptionWorkspaceUsesAuthorizedTenantQueryAndAccessibleFacts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var componentsRoot = Path.Combine(repositoryRoot, "src", "ProcurePilot.Web", "Components");
        var workspace = File.ReadAllText(Path.Combine(componentsRoot, "Pages", "ExceptionWorkspace.razor"));
        var comparison = File.ReadAllText(Path.Combine(componentsRoot, "Shared", "ExceptionComparison.razor"));

        Assert.Contains("IExceptionWorkspaceQueries", workspace, StringComparison.Ordinal);
        Assert.Contains("AuthorizationPolicies.ExceptionView", workspace, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", workspace, StringComparison.Ordinal);
        Assert.Contains("Exception not found", workspace, StringComparison.Ordinal);
        Assert.Contains("read only", workspace, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<dl>", comparison, StringComparison.Ordinal);
        Assert.Contains("Allowed tolerance", comparison, StringComparison.Ordinal);
        Assert.Contains("Math.Abs(Model.VariancePercent)", comparison, StringComparison.Ordinal);
        Assert.DoesNotContain("AI Analysis", workspace, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TODO", workspace, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ProcurePilot.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the ProcurePilot repository root.");
    }

    [GeneratedRegex("#[0-9a-fA-F]{3,8}\\b")]
    private static partial Regex HexColorPattern();
}
