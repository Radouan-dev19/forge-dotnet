using ForgeDotNet.Application;
using ForgeDotNet.Domain;

namespace ForgeDotNet.UnitTests;

public sealed class AssemblyBoundaryTests
{
    [Fact]
    public void DomainHasNoProjectDependencies()
    {
        var projectAssemblies = typeof(DomainAssembly).Assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("ForgeDotNet.", StringComparison.Ordinal) is true);

        Assert.Empty(projectAssemblies);
    }

    [Fact]
    public void ApplicationDependsOnDomainOnly()
    {
        var projectReferences = typeof(ApplicationAssembly).Assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("ForgeDotNet.", StringComparison.Ordinal) is true)
            .Select(reference => reference.Name!)
            .ToArray();

        Assert.Equal(["ForgeDotNet.Domain"], projectReferences);
    }
}
