using System.Net;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.EndToEndTests;

/// <summary>
/// Tout type de contenu publié doit être atteignable par une page servie.
/// </summary>
/// <remarks>
/// <para>
/// Le défaut que cette classe ferme est d'une espèce que rien ne signalait : le validateur de contenu
/// contrôle la structure d'un document, jamais son accessibilité depuis le produit. Deux familles sur
/// dix — 242 fiches d'entretien et 51 cartes d'anglais, soit 293 documents — étaient chargées,
/// validées et comptées dans les instantanés de volume, sans qu'aucune route ne permette de les lire.
/// Un lot de contenu pouvait donc être publié, mesuré et célébré sans jamais atteindre personne.
/// </para>
/// <para>
/// La règle porte sur l'énumération plutôt que sur une liste de routes : un type ajouté à
/// <see cref="ContentDocumentType"/> sans écran fait échouer ce test, et c'est le moment où la
/// question se pose utilement. La correspondance est déclarée ici parce qu'aucune convention ne relie
/// un type à sa route — ce qui est justement pourquoi l'oubli était possible.
/// </para>
/// </remarks>
public sealed class ContentReachabilityWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    /// <summary>Route d'index par laquelle un apprenant atteint chaque famille de contenu.</summary>
    private static readonly Dictionary<ContentDocumentType, string> IndexRoutes = new()
    {
        [ContentDocumentType.Lesson] = "/learn",
        [ContentDocumentType.Curriculum] = "/learn",
        [ContentDocumentType.Exercise] = "/practice",
        [ContentDocumentType.DebugScenario] = "/debug-lab",
        [ContentDocumentType.SqlScenario] = "/sql-lab",
        [ContentDocumentType.Project] = "/projects",
        [ContentDocumentType.Lab] = "/labs",
        [ContentDocumentType.ReviewCardBank] = "/reviews",
        [ContentDocumentType.InterviewQuestion] = "/interviews",
        [ContentDocumentType.EnglishActivity] = "/english",
    };

    [Fact]
    public void EveryPublishedContentTypeDeclaresARoute()
    {
        ContentDocumentType[] missing = Enum.GetValues<ContentDocumentType>()
            .Where(type => !IndexRoutes.ContainsKey(type))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Ces types de contenu n'ont aucune route déclarée, donc aucun apprenant ne peut les lire : "
            + string.Join(", ", missing));
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task EveryDeclaredRouteIsServed(string route)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Les deux familles longtemps invisibles listent réellement leurs documents.
    /// </summary>
    /// <remarks>
    /// Une route qui répond 200 sur une page vide refermerait le défaut en apparence seulement. Ce
    /// test lit donc un titre réellement publié dans chaque index, plutôt que de se contenter du code
    /// de statut.
    /// </remarks>
    [Fact]
    public async Task ThePreparationIndexesListTheirDocumentsAndClaimNoMasteryProof()
    {
        using HttpClient client = factory.CreateClient();

        string interviews = WebUtility.HtmlDecode(await client.GetStringAsync("/interviews"));
        Assert.Contains("/interviews/interview-algo-binary-search-001", interviews, StringComparison.Ordinal);
        Assert.Contains("ne produit de preuve de maîtrise", interviews, StringComparison.Ordinal);
        Assert.Contains("HUMAN_REVIEW.md", interviews, StringComparison.Ordinal);

        string english = WebUtility.HtmlDecode(await client.GetStringAsync("/english"));
        Assert.Contains("/english/english-card-01-written", english, StringComparison.Ordinal);
        Assert.Contains("/english/english-card-01-spoken", english, StringComparison.Ordinal);
        Assert.Contains("ne produit de preuve de maîtrise", english, StringComparison.Ordinal);
    }

    /// <summary>
    /// Une fiche ne livre sa réponse modèle qu'à la demande.
    /// </summary>
    /// <remarks>
    /// Afficher la réponse d'emblée transformerait la préparation en lecture : on saurait reconnaître
    /// la bonne réponse sans jamais avoir essayé de la produire, ce qui est précisément l'illusion que
    /// le protocole d'entretien cherche à éviter.
    /// </remarks>
    [Fact]
    public async Task AnInterviewSheetHidesItsModelAnswerUntilAskedFor()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(
            await client.GetStringAsync("/interviews/interview-algo-binary-search-001"));

        Assert.Contains("Rechercher dans un tableau trié", html, StringComparison.Ordinal);
        Assert.Contains("Révéler les critères et la réponse modèle", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Réduire un intervalle fermé", html, StringComparison.Ordinal);
    }

    public static TheoryData<string> Routes()
    {
        var data = new TheoryData<string>();
        foreach (string route in IndexRoutes.Values.Distinct(StringComparer.Ordinal))
        {
            data.Add(route);
        }

        return data;
    }
}
