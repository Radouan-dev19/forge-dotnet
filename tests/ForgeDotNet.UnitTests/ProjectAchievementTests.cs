using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Projects;

namespace ForgeDotNet.UnitTests;

/// <summary>
/// Rend visible ce qu'une porte exige, ce que le produit sait produire, et ce qui manque à chaque
/// exigence non produite.
/// </summary>
/// <remarks>
/// Le défaut d'origine n'était pas une règle fausse : c'était un chaînon absent, et surtout
/// invisible. <c>MasteryPolicyCatalog</c> déclare vingt-trois clés d'accomplissement ; longtemps,
/// deux seulement avaient un producteur, et les portes B, C et D étaient fermées définitivement sans
/// que rien dans le code ne le signale. Dix clés sont produites aujourd'hui ; treize restent
/// inventoriées avec leur diagnostic.
///
/// Ce test rend l'inventaire obligatoire : toute clé exigée par une porte doit figurer soit parmi les
/// clés produites, soit parmi les clés déclarées sans producteur, avec ce qui lui manque et pourquoi.
/// On ne peut plus ajouter une exigence sans dire qui la satisfait, ni oublier qu'une exigence n'est
/// satisfaite par personne, ni laisser un « il manque vingt et une clés » sans diagnostic.
/// </remarks>
public sealed class ProjectAchievementTests
{
    /// <summary>Clés qu'un producteur du produit sait réellement émettre.</summary>
    private static readonly string[] ProducedAchievementKeys =
    [
        // SqliteMasteryEvidenceSource.AddExamsAsync
        MasteryPolicyCatalog.NinetyMinuteExam,

        // SqliteMasteryEvidenceSource.AddProjectsAsync, sur déclaration du contenu
        MasteryPolicyCatalog.ConsoleProject,

        // SqliteMasteryEvidenceSource.AddProjectsAsync : project-code-review-001 (piste senior S31)
        // déclare la clé code-review et porte des suites d'acceptation qui notent des défauts plantés.
        // C'est le premier producteur au-delà de la porte A.
        MasteryPolicyCatalog.CodeReview,

        // project-orders-database-001 : le bac à sable embarque Microsoft.EntityFrameworkCore et
        // Microsoft.Data.Sqlite parmi ses assemblies approuvées. Ses trois suites exécutent donc du
        // vrai EF Core contre une vraie base — modèle traversé, requêtes comptées par un
        // intercepteur, écriture relue depuis un contexte neuf. L'artefact que la clé nomme est
        // exercé, pas raisonné.
        MasteryPolicyCatalog.EfCore,

        // Six projets vérifiables livrés le 18 août 2026, chacun sur le modèle ci-dessus : la clé
        // est déclarée par le manifeste, les suites s'exécutent dans le bac à sable, et chaque
        // producteur est prouvé de bout en bout hors Docker par ProjectCorrectnessTests.

        // project-validation-pipeline-001 : Validator.TryValidateObject traverse les attributs du
        // vrai pipeline DataAnnotations — celui-là même qu'ASP.NET Core exécute —, l'apprenant
        // l'étend par un attribut personnalisé et projette les manquements dans un contrat 200/422.
        MasteryPolicyCatalog.ValidationAndErrors,

        // project-operations-log-001 : les entrées passent par le vrai ILogger de
        // Microsoft.Extensions.Logging — seuil, caviardage avant émission, corrélation par
        // BeginScope — et le puits capturé est la seule source du résultat.
        MasteryPolicyCatalog.Logs,

        // project-incident-drill-001 : la clé nomme un incident SIMULÉ, et c'est ce que le projet
        // exerce — un service simulé déterministe que l'apprenant détecte, atténue par la bonne
        // action et vérifie rétabli sur les signaux relus, jamais sur l'action posée.
        MasteryPolicyCatalog.SimulatedIncident,

        // project-query-budget-001 : la piste « compter des allers-retours plutôt que chronométrer »
        // est empruntée — le squelette fonctionne mais interroge élément par élément, et l'apprenant
        // doit rendre le même résultat sous un budget de requêtes compté par un intercepteur.
        MasteryPolicyCatalog.Performance,

        // project-abuse-hardening-001 : HMAC recalculé et comparé en temps constant, canonisation
        // de chemin, fenêtre anti-rejeu — éprouvés par des cas cachés d'abus que l'énoncé ne liste
        // pas, ce que le diagnostic de la clé exigeait.
        MasteryPolicyCatalog.Security,

        // project-autonomous-feature-001 : une spécification contractuelle complète, aucun
        // découpage, aucun harnais, aucun indice — la fonctionnalité se livre sur le seul contrat,
        // ce qui est la définition du geste dans les limites déclarées du bac à sable.
        MasteryPolicyCatalog.AutonomousFeature,
    ];

    /// <summary>Ce qui manque à une exigence pour devenir produisible.</summary>
    private enum Blocker
    {
        /// <summary>Un contenu vérifiable côté serveur reste à écrire ou à rendre atteignable.</summary>
        MissingContent,

        /// <summary>Aucune preuve automatique ne peut remplacer un jugement humain.</summary>
        HumanJudgement,
    }

    /// <summary>
    /// Exigence sans producteur, avec ce qui la bloque et pourquoi.
    /// </summary>
    /// <remarks>
    /// La liste était auparavant plate : elle disait « vingt et une clés manquent » sans dire ce qui
    /// manquait à chacune. C'est précisément ce qui a permis au défaut de rester invisible pendant
    /// plusieurs incréments, et ce qui obligeait chaque reprise à refaire le classement de zéro.
    /// </remarks>
    private sealed record Unproduced(string Key, Blocker Blocker, string Reason);

    /// <summary>
    /// Clés qu'aucun producteur n'émet à ce jour. Ce nombre ne peut que descendre.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Principe du classement : un accomplissement ne s'attribue que sur une preuve qui exerce le même
    /// artefact que son intitulé nomme. Les exercices <c>api-*</c>, <c>docker-*</c>, <c>ci-*</c> et
    /// <c>tests-*</c> sont des fonctions pures qui raisonnent <em>sur</em> leur sujet sans le
    /// pratiquer ; en tirer l'accomplissement correspondant fabriquerait un faux signal de maîtrise,
    /// que la politique refuse.
    /// </para>
    /// <para>
    /// Conclusion de l'inventaire : treize clés restent sans producteur — sept attendent un contenu
    /// vérifiable ou un canal de preuve qui n'existe pas encore, six exigent un jugement humain et ne
    /// descendront jamais par du code. Huit en sont sorties : code-review et ef-core d'abord, puis le
    /// 18 août 2026 validation-errors, logs, incident.simulated, performance, security et
    /// feature.autonomous, chacune par un projet vérifiable dédié.
    /// </para>
    /// </remarks>
    private static readonly Unproduced[] UnproducedAchievements =
    [
        new(MasteryPolicyCatalog.ApiFunctional, Blocker.MissingContent,
            "Le bac à sable démarre avec « --network none » et n'approuve aucune assembly d'hébergement HTTP : "
            + "rien n'y sert de requête. Un projet ne pourrait faire décider que des règles — négociation, statut, "
            + "pagination — c'est-à-dire raisonner sur HTTP sans en exercer une seule ligne."),
        new(MasteryPolicyCatalog.UnitTests, Blocker.MissingContent,
            "Le runner invoque une méthode statique nommée d'avance : il ne découvre ni n'exécute des tests écrits par "
            + "l'apprenant. Le faire retourner un rapport d'assertions serait falsifiable — rien n'obligerait les "
            + "assertions à porter sur du code réel — et les implémentations à éprouver seraient visibles dans la "
            + "soumission, donc contournables par une réponse écrite en dur."),
        new(MasteryPolicyCatalog.IntegrationTests, Blocker.MissingContent,
            "Une base réelle est désormais atteignable — project-orders-database-001 le prouve — mais l'artefact que la "
            + "clé nomme reste des tests écrits par l'apprenant, que le runner ne sait pas découvrir. Exercer une base "
            + "n'est pas écrire un test d'intégration."),
        new(MasteryPolicyCatalog.Docker, Blocker.MissingContent,
            "La soumission est du code C# compilé dans un conteneur déjà construit : rien n'y bâtit ni n'y exécute "
            + "d'image. Les exercices docker-* raisonnent sur des couches et des quotas sans en produire aucun."),
        new(MasteryPolicyCatalog.ContinuousIntegration, Blocker.MissingContent,
            "Aucun pipeline ne s'exécute dans le bac à sable, et le workflow du laboratoire ci-delivery tourne hors "
            + "produit : sa réussite n'est collectée par aucun canal que le serveur puisse vérifier."),
        new(MasteryPolicyCatalog.AuthenticationAuthorization, Blocker.MissingContent,
            "Les exercices security-jwt-* font valider un jeton à la main et le laboratoire api-jwt-bearer câble le "
            + "middleware, mais aucun ne produit l'accomplissement : la preuve d'exercice reste une preuve de pratique, "
            + "et le laboratoire s'exécute hors du bac à sable, sans preuve collectée par le serveur."),
        new(MasteryPolicyCatalog.Deployment, Blocker.MissingContent,
            "Le mode Azure est simulé, et son laboratoire ne produit aucune preuve que le serveur collecte."),
        new(MasteryPolicyCatalog.CleanGit, Blocker.HumanJudgement,
            "Juger un historique exige de lire un dépôt réel, auquel le produit local n'a pas accès et ne doit pas en avoir."),
        new(MasteryPolicyCatalog.TenMinutePresentation, Blocker.HumanJudgement,
            "Une présentation orale ne se vérifie pas par une suite de tests, quelle que soit sa trace écrite."),
        new(MasteryPolicyCatalog.MockInterview, Blocker.HumanJudgement,
            "Un entretien suppose un interlocuteur qui relance et conteste ; aucun contenu ne le remplace."),
        new(MasteryPolicyCatalog.PragmaticArchitecture, Blocker.HumanJudgement,
            "La qualité d'une note de décision se juge sur son argumentation, pas sur un résultat calculable."),
        new(MasteryPolicyCatalog.English, Blocker.HumanJudgement,
            "L'expression orale et écrite demande un lecteur ; les cinquante cartes d'anglais, appariées écrit/oral "
            + "et complétées d'un glossaire, sont auto-évaluées."),
        new(MasteryPolicyCatalog.FinalDefense, Blocker.HumanJudgement,
            "La défense finale est une performance orale devant un jury, par construction."),
    ];

    private static string[] UnproducedAchievementKeys =>
        UnproducedAchievements.Select(item => item.Key).ToArray();

    /// <summary>Plafond de clés sans producteur. Il ne peut que descendre.</summary>
    /// <remarks>
    /// Descendu à treize le 18 août 2026, par six projets vérifiables livrés dans le même incrément —
    /// validation-errors, logs, incident.simulated, performance, security et feature.autonomous. Les
    /// premiers producteurs au-delà de la porte A furent project-code-review-001 puis
    /// project-orders-database-001 ; les six nouveaux suivent exactement leur modèle : clé déclarée
    /// par le manifeste, suites exécutées dans le bac à sable, trajet prouvé hors Docker.
    ///
    /// Ce plafond ne descend que d'autant de clés réellement produites. Un producteur qui ne peut jamais
    /// se déclencher, ou qui se déclenche sur une preuve n'exerçant pas l'artefact que la clé nomme,
    /// ferait descendre ce plafond sans rien débloquer : c'est exactement le faux signal que ce test
    /// existe pour empêcher.
    /// </remarks>
    private const int MaximumUnproducedKeys = 13;

    [Fact]
    public void EveryGateRequirementIsEitherProducedOrDeclaredUnproduced()
    {
        string[] required = MasteryPolicyCatalog.Version1.Gates
            .SelectMany(gate => gate.Requirements)
            .Where(requirement => requirement.AchievementKey is not null)
            .Select(requirement => requirement.AchievementKey!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        string[] inventoried = [.. ProducedAchievementKeys, .. UnproducedAchievementKeys];
        string[] undeclared = required.Except(inventoried, StringComparer.Ordinal).ToArray();
        string[] orphans = inventoried.Except(required, StringComparer.Ordinal).ToArray();

        Assert.True(
            undeclared.Length == 0,
            "Ces exigences de porte ne sont ni produites ni déclarées sans producteur : "
            + string.Join(", ", undeclared));
        Assert.True(
            orphans.Length == 0,
            "Ces clés inventoriées ne sont exigées par aucune porte : " + string.Join(", ", orphans));
        Assert.Equal(
            inventoried.Length,
            inventoried.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheNumberOfRequirementsWithoutAProducerNeverGrows()
    {
        Assert.True(
            UnproducedAchievementKeys.Length <= MaximumUnproducedKeys,
            $"{UnproducedAchievementKeys.Length} exigences sans producteur, pour un plafond de "
            + $"{MaximumUnproducedKeys}.");
    }

    /// <summary>
    /// Chaque exigence sans producteur dit ce qui lui manque, et le dit utilement.
    /// </summary>
    /// <remarks>
    /// Sans cette règle, l'inventaire redeviendrait une liste de clés : on saurait combien il en
    /// manque, jamais pourquoi, et la reprise suivante devrait refaire le diagnostic de zéro.
    /// </remarks>
    [Fact]
    public void EveryUnproducedRequirementCarriesAnActionableReason()
    {
        var offenders = new List<string>();

        foreach (Unproduced item in UnproducedAchievements)
        {
            if (!Enum.IsDefined(item.Blocker))
            {
                offenders.Add($"{item.Key} : blocage inconnu.");
            }

            // Quarante caractères écartent « à faire » sans imposer un roman : la justification doit
            // nommer le livrable manquant ou la raison du refus, ce qui ne tient pas en trois mots.
            if (item.Reason.Trim().Length < 40)
            {
                offenders.Add($"{item.Key} : justification trop courte pour être actionnable.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
        Assert.Equal(
            UnproducedAchievements.Length,
            UnproducedAchievements.Select(item => item.Reason).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Le classement distingue ce qu'un contenu débloquera de ce qu'aucun code ne débloquera.
    /// </summary>
    /// <remarks>
    /// La distinction n'est pas cosmétique : elle dit à une reprise future où son effort porte. Traiter
    /// les six exigences humaines comme une dette technique conduirait à fabriquer une preuve
    /// automatique de ce qui ne s'automatise pas — un entretien noté par une suite de tests.
    ///
    /// Ce qui existe pour elles est le protocole de revue par un tiers, <c>docs/HUMAN_REVIEW.md</c>,
    /// branché au produit depuis le 18 août 2026 : la page /human-review enregistre l'attestation
    /// d'un relecteur nommé sous <c>MasteryVerificationKind.HumanAttestation</c>, un troisième type
    /// de preuve que les règles admettent exclusivement pour ces six clés. Elles restent dans cet
    /// inventaire parce qu'il compte les producteurs <em>automatiques</em> : aucun code ne les
    /// vérifiera jamais, et <c>ManualDeclaration</c> — la parole de l'apprenant seul — vaut toujours
    /// zéro.
    /// </remarks>
    [Fact]
    public void TheInventorySeparatesMissingContentFromHumanJudgement()
    {
        int missingContent = UnproducedAchievements.Count(item => item.Blocker == Blocker.MissingContent);
        int humanJudgement = UnproducedAchievements.Count(item => item.Blocker == Blocker.HumanJudgement);

        Assert.Equal(7, missingContent);
        Assert.Equal(6, humanJudgement);
        Assert.Equal(UnproducedAchievements.Length, missingContent + humanJudgement);
    }

    /// <summary>
    /// Seules les clés nommément produites ont un producteur au-delà de la porte A.
    /// </summary>
    /// <remarks>
    /// La forme d'origine — « aucune exigence au-delà de la porte A n'a de producteur » — a cessé
    /// d'être vraie le jour où le premier producteur est apparu, et l'énoncer par une liste
    /// d'exceptions à étendre à chaque fois manquait le point. La règle utile est le classement :
    /// chaque exigence des portes B, C et D est soit produite, soit inventoriée avec son diagnostic,
    /// jamais les deux et jamais ni l'une ni l'autre. Une clé qui gagne un producteur doit donc
    /// quitter l'inventaire dans le même incrément — c'est ce qui empêche un producteur muet, ajouté
    /// sans contenu qui le déclenche, de faire croire qu'une porte s'est ouverte.
    /// </remarks>
    [Theory]
    [InlineData(MasteryGate.B)]
    [InlineData(MasteryGate.C)]
    [InlineData(MasteryGate.D)]
    public void EveryRequirementBeyondGateAIsClassifiedExactlyOnce(MasteryGate gate)
    {
        string[] keys = MasteryPolicyCatalog.Version1.Gates
            .Single(item => item.Gate == gate)
            .Requirements
            .Where(item => item.AchievementKey is not null)
            .Select(item => item.AchievementKey!)
            .ToArray();

        Assert.NotEmpty(keys);

        var unclassified = new List<string>();
        var doubleClassified = new List<string>();

        foreach (string key in keys)
        {
            bool produced = ProducedAchievementKeys.Contains(key, StringComparer.Ordinal);
            bool unproduced = UnproducedAchievementKeys.Contains(key, StringComparer.Ordinal);

            if (produced && unproduced)
            {
                doubleClassified.Add(key);
            }
            else if (!produced && !unproduced)
            {
                unclassified.Add(key);
            }
        }

        Assert.True(
            unclassified.Count == 0,
            $"Porte {gate} : ni produites ni inventoriées — " + string.Join(", ", unclassified));
        Assert.True(
            doubleClassified.Count == 0,
            $"Porte {gate} : produites ET inventoriées comme manquantes — "
            + string.Join(", ", doubleClassified));
    }

    /// <summary>
    /// Huit clés ont un producteur au-delà de la porte A, et on les nomme.
    /// </summary>
    /// <remarks>
    /// Figer ce compte nommé rend visible chaque avancée réelle et interdit qu'elle passe inaperçue.
    /// Chaque producteur exerce l'artefact que sa clé nomme — une revue qui note des défauts plantés,
    /// des requêtes exécutées contre une vraie base, un pipeline de validation traversé, un journal
    /// capturé au puits, un incident simulé conduit, un budget de requêtes tenu, une défense éprouvée
    /// par des abus non énumérés, une fonctionnalité livrée sur contrat — condition d'admission.
    /// </remarks>
    [Fact]
    public void OnlyTheNamedKeysHaveAProducerBeyondGateA()
    {
        string[] beyondGateA = MasteryPolicyCatalog.Version1.Gates
            .Where(gate => gate.Gate != MasteryGate.A)
            .SelectMany(gate => gate.Requirements)
            .Where(requirement => requirement.AchievementKey is not null)
            .Select(requirement => requirement.AchievementKey!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        string[] produced = beyondGateA
            .Where(key => ProducedAchievementKeys.Contains(key, StringComparer.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                MasteryPolicyCatalog.CodeReview,
                MasteryPolicyCatalog.EfCore,
                MasteryPolicyCatalog.AutonomousFeature,
                MasteryPolicyCatalog.SimulatedIncident,
                MasteryPolicyCatalog.Logs,
                MasteryPolicyCatalog.Performance,
                MasteryPolicyCatalog.Security,
                MasteryPolicyCatalog.ValidationAndErrors,
            ],
            produced);
    }

    /// <summary>
    /// La porte A dépend de clés désormais produites : elle est franchissable par le travail.
    /// </summary>
    [Fact]
    public void GateARequiresOnlyKeysThatHaveAProducer()
    {
        string[] gateAKeys = MasteryPolicyCatalog.Version1.Gates
            .Single(gate => gate.Gate == MasteryGate.A)
            .Requirements
            .Where(requirement => requirement.AchievementKey is not null)
            .Select(requirement => requirement.AchievementKey!)
            .ToArray();

        Assert.All(gateAKeys, key => Assert.Contains(key, ProducedAchievementKeys, StringComparer.Ordinal));
    }

    /// <summary>
    /// Une soumission ne peut pas se déclarer réussie sans avoir été exécutée.
    /// </summary>
    /// <remarks>
    /// C'est la garantie qui autorise le producteur à en tirer un accomplissement : le refus vit
    /// dans le domaine, pas dans l'interface, et aucun appelant ne peut le contourner.
    /// </remarks>
    [Theory]
    [InlineData(false, 3, 3, 12, 12, "non exécutée")]
    [InlineData(true, 3, 2, 12, 8, "une suite en échec")]
    [InlineData(true, 0, 0, 0, 0, "aucune suite")]
    [InlineData(true, 3, 3, 0, 0, "aucun test rapporté")]
    public void ASuccessfulSubmissionMustBeProvenByEverySuite(
        bool verified,
        int totalSuites,
        int passedSuites,
        int totalTests,
        int passedTests,
        string reason)
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Succeeded, verified, totalSuites, passedSuites, totalTests, passedTests);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(submission.Validate);

        Assert.Contains("prouvée", error.Message, StringComparison.Ordinal);
        Assert.False(submission.ProducesEvidence, reason);
    }

    [Fact]
    public void AManualDeclarationNeverClaimsToBeAutomaticallyVerified()
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Declared, verified: true, 3, 3, 12, 12);

        Assert.Throws<InvalidOperationException>(submission.Validate);
    }

    [Fact]
    public void AManualSubmissionIsRecordedButProducesNoEvidence()
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Declared, verified: false, 3, 0, 0, 0);

        submission.Validate();

        Assert.False(submission.ProducesEvidence);
    }

    [Fact]
    public void AFullyVerifiedSubmissionProducesEvidence()
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Succeeded, verified: true, 3, 3, 12, 12);

        submission.Validate();

        Assert.True(submission.ProducesEvidence);
    }

    /// <summary>
    /// Un projet sans clé déclarée ne prouve rien, même vérifié de bout en bout.
    /// </summary>
    [Fact]
    public void AVerifiableProjectWithoutADeclaredKeyProducesNoAchievement()
    {
        Project withoutKey = ProjectWith(achievementKey: null);
        Project withKey = ProjectWith(MasteryPolicyCatalog.ConsoleProject);

        Assert.True(withoutKey.IsVerifiable);
        Assert.False(withoutKey.ProducesAchievement);
        Assert.True(withKey.ProducesAchievement);
    }

    private static ProjectSubmission Submission(
        ProjectSubmissionStatus status,
        bool verified,
        int totalSuites,
        int passedSuites,
        int totalTests,
        int passedTests) => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "project-collections-library-001",
            2,
            "sha256:" + new string('a', 64),
            "sha256:" + new string('b', 64),
            status,
            totalSuites,
            passedSuites,
            totalTests,
            passedTests,
            verified,
            DateTimeOffset.UnixEpoch.AddDays(1));

    private static Project ProjectWith(string? achievementKey) => new(
        "project-collections-library-001",
        2,
        "Bibliothèque de collections",
        2,
        [2],
        ["csharp.collections"],
        8,
        "brief",
        [new ProjectStarterFile("Submission.cs", "public static class Submission { }")],
        4,
        [new ProjectMilestone("normalisation", "Assainir", "preuve", ["critère"])],
        [new ProjectRubricCriterion("critère", 1m, "preuve observable")],
        [new ProjectAcceptanceSuite("normalisation", "normalisation/")],
        ["erreur fréquente"],
        achievementKey,
        "sha256:" + new string('a', 64));
}
