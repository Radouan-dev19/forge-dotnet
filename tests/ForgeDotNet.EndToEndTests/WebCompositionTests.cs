using ForgeDotNet.Web;

namespace ForgeDotNet.EndToEndTests;

public sealed class WebCompositionTests
{
    [Fact]
    public void WebReferencesTheCompositionProjects()
    {
        var projectReferences = typeof(WebAssembly).Assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("ForgeDotNet.", StringComparison.Ordinal) is true)
            .Select(reference => reference.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["ForgeDotNet.Application", "ForgeDotNet.CodeRunner", "ForgeDotNet.Domain", "ForgeDotNet.Infrastructure"],
            projectReferences);
    }
}
