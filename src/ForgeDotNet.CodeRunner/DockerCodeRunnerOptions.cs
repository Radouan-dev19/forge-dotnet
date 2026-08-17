using System.Text.RegularExpressions;
using ForgeDotNet.Application.CodeRunner;

namespace ForgeDotNet.CodeRunner;

public sealed record DockerRunSpecification(string SuiteId, string? SuiteDefinition = null);

public interface IDockerRunSpecificationSource
{
    ValueTask<DockerRunSpecification?> GetAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class NoDockerRunSpecificationSource : IDockerRunSpecificationSource
{
    public ValueTask<DockerRunSpecification?> GetAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        CodeRunContract.ValidateRequest(request);
        return ValueTask.FromResult<DockerRunSpecification?>(null);
    }
}

public sealed partial record DockerCodeRunnerOptions
{
    public const long Mebibyte = 1024 * 1024;
    public const string RequiredContainerUser = "1654:1654";
    public const string RunnerLabel = "forge-dotnet.runner";
    public const string RunnerLabelValue = "true";

    public string DockerExecutablePath { get; init; } = "docker";

    public string DockerContext { get; init; } = "desktop-linux";

    public string ImageReference { get; init; } = string.Empty;

    public string WorkspaceRootPath { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ForgeDotNet",
        "runner-workspaces");

    public int MaximumConcurrency { get; init; } = 2;

    public double CpuCount { get; init; } = 1.0;

    public long MemoryBytes { get; init; } = 512 * Mebibyte;

    public int PidsLimit { get; init; } = 64;

    public long WorkspaceBytes { get; init; } = 64 * Mebibyte;

    public TimeSpan CompilationTimeout { get; init; } = TimeSpan.FromSeconds(25);

    public TimeSpan TestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan DockerControlTimeout { get; init; } = TimeSpan.FromSeconds(15);

    [GeneratedRegex("^sha256:[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ImageReferencePattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex DockerContextPattern();

    public void Validate()
    {
        if (!string.Equals(DockerExecutablePath, "docker", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(DockerExecutablePath, "docker.exe", StringComparison.OrdinalIgnoreCase))
        {
            if (!Path.IsPathFullyQualified(DockerExecutablePath)
                || !string.Equals(Path.GetFileName(DockerExecutablePath), "docker.exe", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(DockerExecutablePath))
            {
                throw new InvalidDataException("Le chemin du client Docker doit désigner docker.exe ou utiliser la commande docker.");
            }
        }

        if (!DockerContextPattern().IsMatch(DockerContext ?? string.Empty))
        {
            throw new InvalidDataException("Le contexte Docker est invalide.");
        }

        if (!ImageReferencePattern().IsMatch(ImageReference ?? string.Empty))
        {
            // Le format seul ne suffit pas à dépanner : la valeur manque presque toujours parce que
            // l'image n'a jamais été construite, et sa construction n'est pas devinable — le contexte
            // est src/ForgeDotNet.CodeRunner/Container, pas la racine du dépôt. Nommer le script
            // évite que l'installation s'arrête sur une exigence de forme sans chemin de sortie.
            throw new InvalidDataException(
                "L’image runner doit être référencée par un identifiant immuable sha256 complet, jamais par une "
                + "étiquette. Construisez-la avec scripts/build-code-runner.ps1 : il rend la référence à "
                + "configurer dans CodeRunner:Docker:ImageReference.");
        }

        if (string.IsNullOrWhiteSpace(WorkspaceRootPath) || !Path.IsPathFullyQualified(WorkspaceRootPath))
        {
            throw new InvalidDataException("La racine des workspaces runner doit être un chemin absolu.");
        }

        string root = Path.GetFullPath(WorkspaceRootPath);
        string? volumeRoot = Path.GetPathRoot(root);
        if (string.Equals(root.TrimEnd(Path.DirectorySeparatorChar), volumeRoot?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
            || root.Contains(',', StringComparison.Ordinal))
        {
            throw new InvalidDataException("La racine des workspaces runner est trop large ou incompatible avec un montage Docker sûr.");
        }

        if (MaximumConcurrency is < 1 or > 4
            || CpuCount is < 0.1 or > 1.0
            || MemoryBytes is < 128 * Mebibyte or > 512 * Mebibyte
            || PidsLimit is < 16 or > 64
            || WorkspaceBytes is < 16 * Mebibyte or > 128 * Mebibyte
            || CompilationTimeout < TimeSpan.FromSeconds(2)
            || CompilationTimeout > TimeSpan.FromSeconds(30)
            || TestTimeout < TimeSpan.FromSeconds(1)
            || TestTimeout > TimeSpan.FromSeconds(30)
            || DockerControlTimeout < TimeSpan.FromSeconds(2)
            || DockerControlTimeout > TimeSpan.FromSeconds(30))
        {
            throw new InvalidDataException("Une limite Docker sort de la plage de sécurité autorisée.");
        }
    }
}
