using System.Globalization;
using System.Text;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Harness;

/// <summary>
/// Registre de preuves d'un persona : une capture horodatée et une ligne Markdown par étape.
/// Le registre vit sous artifacts/personas/&lt;run&gt;/&lt;persona&gt;/, non versionné.
/// </summary>
public sealed class EvidenceRegistry
{
    private readonly string _directory;
    private readonly string _registryPath;
    private int _stepIndex;

    public EvidenceRegistry(string personaId, string personaTitle)
    {
        _directory = Path.Combine(PersonaPaths.ArtifactsRoot, personaId);
        Directory.CreateDirectory(_directory);
        _registryPath = Path.Combine(_directory, "REGISTRE.md");
        File.WriteAllText(
            _registryPath,
            $"# Registre de preuves — {personaTitle}\n\n"
            + $"Exécution {PersonaPaths.RunStamp} (UTC). Chaque étape porte sa capture horodatée et\n"
            + "l'assertion vérifiée sur l'état observé ou persistant.\n\n"
            + "| # | Horodatage UTC | Étape | Preuve | Capture |\n|---|---|---|---|---|\n",
            new UTF8Encoding(false));
    }

    public string Directory1 => _directory;

    /// <summary>Capture d'écran horodatée plus ligne de registre.</summary>
    public async Task CaptureAsync(IPage page, string step, string proof)
    {
        int index = ++_stepIndex;
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss.fff", CultureInfo.InvariantCulture);
        string fileName = $"{index:00}-{stamp}.png";
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_directory, fileName),
            FullPage = true,
        });
        AppendRow(index, stamp, step, proof, fileName);
    }

    /// <summary>Ligne de registre sans capture : assertion sur l'état persistant ou limite déclarée.</summary>
    public void Note(string step, string proof)
    {
        int index = ++_stepIndex;
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss.fff", CultureInfo.InvariantCulture);
        AppendRow(index, stamp, step, proof, "—");
    }

    /// <summary>Conclusion du persona, ajoutée au pied du registre.</summary>
    public void Conclude(string verdict)
    {
        File.AppendAllText(
            _registryPath,
            $"\n## Conclusion\n\n{verdict}\n",
            new UTF8Encoding(false));
    }

    private void AppendRow(int index, string stamp, string step, string proof, string capture)
    {
        static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
        File.AppendAllText(
            _registryPath,
            $"| {index} | {stamp} | {Cell(step)} | {Cell(proof)} | {Cell(capture)} |\n",
            new UTF8Encoding(false));
    }
}
