using ForgeDotNet.Infrastructure;

namespace ForgeDotNet.IntegrationTests;

public sealed class AssemblyCompositionTests
{
    [Fact]
    public void InfrastructureReferencesApplicationAndDomainOnly()
    {
        var projectReferences = typeof(InfrastructureAssembly).Assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("ForgeDotNet.", StringComparison.Ordinal) is true)
            .Select(reference => reference.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["ForgeDotNet.Application", "ForgeDotNet.Domain"], projectReferences);
    }
}
