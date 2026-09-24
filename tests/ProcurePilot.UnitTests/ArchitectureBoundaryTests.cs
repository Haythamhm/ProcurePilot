namespace ProcurePilot.UnitTests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        var references = typeof(Domain.AssemblyReference).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.DoesNotContain("ProcurePilot.Application", references);
        Assert.DoesNotContain("ProcurePilot.Infrastructure", references);
        Assert.DoesNotContain("ProcurePilot.Web", references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrWeb()
    {
        var references = typeof(Application.AssemblyReference).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.DoesNotContain("ProcurePilot.Infrastructure", references);
        Assert.DoesNotContain("ProcurePilot.Web", references);
    }
}
