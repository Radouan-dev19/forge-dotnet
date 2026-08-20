using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ForgeDotNet.PersonaTests.Harness;

/// <summary>Mode d'exécution du CodeRunner demandé pour un persona.</summary>
public enum PersonaRunnerMode
{
    Manual,
    Docker,
}

/// <summary>
/// Une instance réelle de l'application, démarrée en processus enfant sur un port libre, avec un
/// dossier de données SQLite dédié et jetable. Le persona la pilote ensuite au navigateur.
/// </summary>
public sealed class ForgeAppInstance : IAsyncDisposable
{
    private Process? _process;

    private ForgeAppInstance(string dataDirectory, bool ownsDataDirectory)
    {
        DataDirectory = dataDirectory;
        OwnsDataDirectory = ownsDataDirectory;
    }

    public string DataDirectory { get; }

    public bool OwnsDataDirectory { get; }

    public string BaseUrl { get; private set; } = string.Empty;

    public string DatabasePath => Path.Combine(DataDirectory, "forge-dotnet.db");

    public static async Task<ForgeAppInstance> StartAsync(
        string personaId,
        PersonaRunnerMode runnerMode,
        bool sqlLabEnabled = false,
        string? existingDataDirectory = null)
    {
        string dataDirectory = existingDataDirectory ?? Path.Combine(
            Path.GetTempPath(),
            "forge-personas",
            $"{personaId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dataDirectory);
        var instance = new ForgeAppInstance(dataDirectory, ownsDataDirectory: existingDataDirectory is null);
        await instance.LaunchAsync(runnerMode, sqlLabEnabled);
        return instance;
    }

    /// <summary>Arrête le processus sans supprimer les données : P7 redémarre sur le même dossier.</summary>
    public async Task StopAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    /// <summary>Redémarre l'application sur les mêmes données, éventuellement dans un autre mode.</summary>
    public async Task RestartAsync(PersonaRunnerMode runnerMode, bool sqlLabEnabled = false)
    {
        await StopAsync();
        await LaunchAsync(runnerMode, sqlLabEnabled);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (OwnsDataDirectory && Directory.Exists(DataDirectory))
        {
            try
            {
                Directory.Delete(DataDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Un antivirus ou un descripteur tardif peut retenir un fichier : le dossier vit
                // sous le répertoire temporaire du système, qui reste nettoyable.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private async Task LaunchAsync(PersonaRunnerMode runnerMode, bool sqlLabEnabled)
    {
        if (!File.Exists(PersonaPaths.WebAssemblyPath))
        {
            throw new InvalidOperationException(
                $"Application Web non compilée : {PersonaPaths.WebAssemblyPath}. Lancer `dotnet build` d'abord.");
        }

        int port = ReserveFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = PersonaPaths.RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(PersonaPaths.WebAssemblyPath);
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        // L'environnement Development sert les ressources statiques (dont blazor.web.js) depuis le
        // manifeste de build : indispensable pour charger le circuit interactif hors publication.
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["LocalData__DirectoryPath"] = DataDirectory;
        startInfo.Environment["CodeRunner__Mode"] = runnerMode == PersonaRunnerMode.Docker ? "Docker" : "Manual";
        if (runnerMode == PersonaRunnerMode.Docker)
        {
            startInfo.Environment["CodeRunner__Docker__ImageReference"] = await ResolveRunnerImageAsync();
        }

        startInfo.Environment["SqlLab__Enabled"] = sqlLabEnabled ? "true" : "false";

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Le processus de l'application n'a pas pu démarrer.");
        _ = _process.StandardOutput.ReadToEndAsync();
        _ = _process.StandardError.ReadToEndAsync();
        await WaitForHealthAsync();
    }

    private async Task WaitForHealthAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(90);
        Exception? lastFailure = null;
        while (DateTime.UtcNow < deadline)
        {
            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"L'application s'est arrêtée au démarrage (code {_process.ExitCode}).");
            }

            try
            {
                using HttpResponseMessage response = await client.GetAsync($"{BaseUrl}/health");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException exception)
            {
                lastFailure = exception;
            }
            catch (TaskCanceledException exception)
            {
                lastFailure = exception;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"L'application n'a pas répondu sur {BaseUrl}/health.", lastFailure);
    }

    private static int ReserveFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Résout la référence immuable de l'image runner comme le fait la suite d'intégration :
    /// par inspection du démon Docker local, jamais par une valeur codée en dur.
    /// </summary>
    private static async Task<string> ResolveRunnerImageAsync()
    {
        foreach (string tag in new[] { "forge-dotnet-runner:local", "forge-dotnet-runner:test" })
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string argument in new[] { "image", "inspect", tag, "--format", "{{.Id}}" })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using Process? probe = Process.Start(startInfo);
            if (probe is null)
            {
                continue;
            }

            string output = (await probe.StandardOutput.ReadToEndAsync()).Trim();
            await probe.WaitForExitAsync();
            if (probe.ExitCode == 0
                && output.StartsWith("sha256:", StringComparison.Ordinal)
                && output.Length == 71)
            {
                return output;
            }
        }

        throw new InvalidOperationException(
            "Aucune image runner (forge-dotnet-runner:local ou :test) : lancer scripts/build-code-runner.ps1.");
    }
}
