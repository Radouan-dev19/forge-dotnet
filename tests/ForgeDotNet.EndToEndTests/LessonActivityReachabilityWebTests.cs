using System.Net;
using System.Text.RegularExpressions;

namespace ForgeDotNet.EndToEndTests;

/// <summary>
/// Chaque activité citée par une leçon publiée doit être atteignable d'un clic depuis la page servie.
/// </summary>
/// <remarks>
/// <see cref="ContentReachabilityWebTests"/> ferme le défaut au niveau des familles : une route d'index
/// par type de document. Ce test descend au niveau du document : il lit chaque <c>lesson.md</c> des deux
/// parcours, relève les identifiants d'activité cités en code inline qui existent réellement sur le
/// disque (exercices, scénarios SQL et de débogage, projets, laboratoires), puis exige que la page de la
/// leçon porte un lien vers chacun. C'est le trou par lequel passaient la piste senior — jamais reliée —
/// et les laboratoires, cités par chemin de fichier.
/// </remarks>
public sealed partial class LessonActivityReachabilityWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ContentRoot = Path.Combine(RepositoryRoot, "content");

    [Fact]
    public async Task EveryActivityCitedByAPublishedLessonIsLinkedFromItsPage()
    {
        using HttpClient client = factory.CreateClient();
        var missing = new List<string>();
        int linkedCitations = 0;

        foreach ((string lessonId, string route, string markdown) in PublishedLessons())
        {
            string[] expectedHrefs = CitedActivityHrefs(markdown).Distinct(StringComparer.Ordinal).ToArray();
            if (expectedHrefs.Length == 0)
            {
                continue;
            }

            using HttpResponseMessage response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            foreach (string href in expectedHrefs)
            {
                if (html.Contains($"href=\"{href}\"", StringComparison.Ordinal))
                {
                    linkedCitations++;
                }
                else
                {
                    missing.Add($"{lessonId} → {href}");
                }
            }
        }

        Assert.True(linkedCitations > 40, $"Trop peu de citations reliées ({linkedCitations}) : le relevé ne lit probablement pas les leçons.");
        Assert.True(
            missing.Count == 0,
            "Activités citées par une leçon mais sans lien sur sa page :" + Environment.NewLine
            + string.Join(Environment.NewLine, missing));
    }

    /// <summary>Leçons des deux parcours, avec la route qui les sert.</summary>
    private static IEnumerable<(string LessonId, string Route, string Markdown)> PublishedLessons()
    {
        string lessonsRoot = Path.Combine(ContentRoot, "reference", "curriculum", "lessons");
        foreach (string manifest in Directory.GetFiles(lessonsRoot, "lesson.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            string lessonId = Path.GetFileName(Path.GetDirectoryName(manifest)!);
            string markdown = File.ReadAllText(Path.Combine(Path.GetDirectoryName(manifest)!, "lesson.md"));
            string route = lessonId.StartsWith("senior-", StringComparison.Ordinal)
                ? $"/learn-senior/{lessonId}"
                : $"/learn/{lessonId}";
            yield return (lessonId, route, markdown);
        }
    }

    /// <summary>
    /// Traduit chaque citation en code inline vers la page attendue, uniquement lorsque l'activité
    /// existe sur le disque : un mot de code ordinaire n'est jamais une activité.
    /// </summary>
    private static IEnumerable<string> CitedActivityHrefs(string markdown)
    {
        foreach (Match match in InlineCodeRegex().Matches(markdown))
        {
            string code = match.Groups["code"].Value;
            Match lab = LabPathRegex().Match(code);
            if (lab.Success && Directory.Exists(Path.Combine(ContentRoot, "labs", lab.Groups["lab"].Value)))
            {
                yield return $"/labs/{lab.Groups["lab"].Value}";
                continue;
            }

            if (!IdentifierRegex().IsMatch(code))
            {
                continue;
            }

            if (Directory.Exists(Path.Combine(ContentRoot, "reference", "exercises", code)))
            {
                yield return $"/practice/{code}";
            }
            else if (Directory.Exists(Path.Combine(ContentRoot, "sql", code))
                && File.ReadAllText(Path.Combine(ContentRoot, "sql", code, "tests", "contract.json"))
                    .Contains("\"mode\": \"sql\"", StringComparison.Ordinal))
            {
                yield return $"/sql-lab?scenario={code}";
            }
            else if (Directory.Exists(Path.Combine(ContentRoot, "reference", "debugging", code)))
            {
                yield return $"/debug-lab/{code}";
            }
            else if (Directory.Exists(Path.Combine(ContentRoot, "reference", "projects", code)))
            {
                yield return $"/projects/{code}";
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Racine du dépôt introuvable.");
    }

    [GeneratedRegex(@"`(?<code>[^`\n]{1,300})`", RegexOptions.CultureInvariant)]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*-\d{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^content/labs/(?<lab>[a-z0-9]+(?:-[a-z0-9]+)*)(?:/.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex LabPathRegex();
}
