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

    [Fact]
    public void ContainerImagePackagesEveryContentDirectoryLoadedAtStartup()
    {
        string repositoryRoot = FindRepositoryRoot();
        string dockerfile = File.ReadAllText(
            Path.Combine(repositoryRoot, "src", "ForgeDotNet.Web", "Dockerfile"));
        string dockerIgnore = File.ReadAllText(Path.Combine(repositoryRoot, ".dockerignore"));

        Assert.Contains("apk add --no-cache icu-libs tzdata", dockerfile, StringComparison.Ordinal);

        string[] requiredDirectories = ["schemas", "reference", "diagnostic", "planning", "exams", "sql"];
        foreach (string directory in requiredDirectories)
        {
            Assert.Contains(
                $"COPY content/{directory}/ ./content/{directory}/",
                dockerfile,
                StringComparison.Ordinal);
            Assert.Contains($"!content/{directory}/", dockerIgnore, StringComparison.Ordinal);
            Assert.Contains($"!content/{directory}/**", dockerIgnore, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ForgeDotNet.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("La racine du dépôt Forge.NET est introuvable.");
    }
}
