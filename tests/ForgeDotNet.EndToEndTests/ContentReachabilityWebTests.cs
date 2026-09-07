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
        [ContentDocumentType.CareerGuide] = "/career",
        [ContentDocumentType.AiGuide] = "/ai",
        [ContentDocumentType.CloudGuide] = "/cloud",
        [ContentDocumentType.WeekZeroGuide] = "/learn",
        [ContentDocumentType.PrepGuide] = "/prep",
        [ContentDocumentType.TheoryQuizBank] = "/thib",
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

        // Le kit carrière avait exactement ce défaut : publié, compté, servi par aucune route.
        string career = WebUtility.HtmlDecode(await client.GetStringAsync("/career"));
        foreach (string guideId in new[]
        {
            "career-cv-evidence-001", "career-star-workbook-001", "career-application-tracker-001",
            "career-negotiation-guide-001", "career-post-hire-plan-001",
        })
        {
            Assert.Contains($"/career/{guideId}", career, StringComparison.Ordinal);
        }

        Assert.Contains("ne produit de preuve de maîtrise", career, StringComparison.Ordinal);
        Assert.Contains("Export-CareerEvidence.ps1", career, StringComparison.Ordinal);

        // Le chapitre IA est hors parcours et son index tient la ligne du contrat : sans IA
        // sur les preuves mesurées.
        string ai = WebUtility.HtmlDecode(await client.GetStringAsync("/ai"));
        foreach (string guideId in new[]
        {
            "ai-modele-mental-001", "ai-economie-tokens-001", "ai-parametrage-001",
            "ai-skills-001", "ai-agents-sous-agents-001", "ai-boucle-quotidienne-001",
        })
        {
            Assert.Contains($"/ai/{guideId}", ai, StringComparison.Ordinal);
        }

        Assert.Contains("ne produit de preuve de maîtrise", ai, StringComparison.Ordinal);
        Assert.Contains("sans IA", ai, StringComparison.Ordinal);
        Assert.Contains("hors parcours", ai, StringComparison.Ordinal);

        // Le chapitre Cloud est hors parcours et distinct du bloc Azure noté.
        string cloud = WebUtility.HtmlDecode(await client.GetStringAsync("/cloud"));
        foreach (string guideId in new[]
        {
            "cloud-fondamentaux-001", "cloud-panorama-azure-001", "cloud-conteneurs-orchestration-001",
            "cloud-kubernetes-concepts-001", "cloud-kubernetes-managed-aks-001", "cloud-securite-couts-001",
        })
        {
            Assert.Contains($"/cloud/{guideId}", cloud, StringComparison.Ordinal);
        }

        Assert.Contains("ne produit de preuve de maîtrise", cloud, StringComparison.Ordinal);
        Assert.Contains("hors parcours", cloud, StringComparison.Ordinal);

        // L'onglet Semaine 0 de la page Apprendre est le plus récent à porter cette exigence :
        // publié et compté ne suffit pas, il doit être listé par une route réellement servie.
        string learn = WebUtility.HtmlDecode(await client.GetStringAsync("/learn"));
        foreach (string guideId in new[]
        {
            "week0-vue-typescript-001", "week0-azure-service-bus-001",
            "week0-azure-devops-001", "week0-iis-001", "week0-microservices-001",
        })
        {
            Assert.Contains($"/learn/week-0/{guideId}", learn, StringComparison.Ordinal);
        }

        Assert.Contains("Semaine 0", learn, StringComparison.Ordinal);

        // L'onglet Entretiens ciblés liste ses dossiers, groupés par sous-thème.
        string prep = WebUtility.HtmlDecode(await client.GetStringAsync("/prep"));
        Assert.Contains("/prep/icube-plan-veille-001", prep, StringComparison.Ordinal);
        Assert.Contains("/prep/icube-scripts-entretien-001", prep, StringComparison.Ordinal);
        Assert.Contains("ICube", prep, StringComparison.Ordinal);
        Assert.Contains("ne produit de preuve de maîtrise", prep, StringComparison.Ordinal);

        // L'onglet Thib liste ses banques de quiz théorique senior.
        string thib = WebUtility.HtmlDecode(await client.GetStringAsync("/thib"));
        Assert.Contains("/thib/thib-runtime-langage-001", thib, StringComparison.Ordinal);
        Assert.Contains("/thib/thib-web-archi-securite-004", thib, StringComparison.Ordinal);
        Assert.Contains("ne produit de preuve de maîtrise", thib, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un guide IA servi porte son texte intégral et tient la frontière : outil de production,
    /// jamais instrument de mesure.
    /// </summary>
    [Fact]
    public async Task AnAiGuidePageServesItsBodyAndClaimsNoMasteryProof()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/ai/ai-modele-mental-001"));

        Assert.Contains("Le modèle mental : contexte, tokens et coût", html, StringComparison.Ordinal);
        Assert.Contains("ne jamais accepter une", html, StringComparison.Ordinal);
        Assert.Contains("aucune preuve de maîtrise", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un guide Cloud servi porte son texte intégral et tient la même frontière que le chapitre IA :
    /// culture de métier, jamais preuve du parcours.
    /// </summary>
    [Fact]
    public async Task ACloudGuidePageServesItsBodyAndClaimsNoMasteryProof()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/cloud/cloud-fondamentaux-001"));

        Assert.Contains("Le cloud en un modèle mental", html, StringComparison.Ordinal);
        Assert.Contains("IaaS", html, StringComparison.Ordinal);
        Assert.Contains("aucune preuve de maîtrise", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un guide de carrière servi porte son texte intégral et rappelle qu'il ne prouve rien.
    /// </summary>
    [Fact]
    public async Task ACareerGuidePageServesItsBodyAndClaimsNoMasteryProof()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/career/career-cv-evidence-001"));

        Assert.Contains("Le CV par preuves", html, StringComparison.Ordinal);
        Assert.Contains("Ce qu'une preuve Forge.NET démontre", html, StringComparison.Ordinal);
        Assert.Contains("aucune preuve de maîtrise", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un guide de la Semaine 0 servi porte son texte intégral et tient la même frontière que les
    /// chapitres IA et Cloud : notion d'appoint, jamais preuve du parcours.
    /// </summary>
    [Fact]
    public async Task AWeekZeroGuidePageServesItsBodyAndClaimsNoMasteryProof()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/learn/week-0/week0-vue-typescript-001"));

        Assert.Contains("Vue.js 3 et TypeScript : lire et écrire un composant", html, StringComparison.Ordinal);
        Assert.Contains("script setup", html, StringComparison.Ordinal);
        Assert.Contains("aucune preuve de maîtrise", html, StringComparison.Ordinal);
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
