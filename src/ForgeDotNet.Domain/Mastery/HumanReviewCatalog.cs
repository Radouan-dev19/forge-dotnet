using System.Text.RegularExpressions;

namespace ForgeDotNet.Domain.Mastery;

/// <summary>Un critère observable d'une grille de revue humaine, binaire : observé ou non.</summary>
public sealed record HumanReviewCriterion(int Number, string Label, bool Mandatory);

/// <summary>
/// Une grille du protocole de revue par un tiers — docs/HUMAN_REVIEW.md, dont ce catalogue est la
/// transcription typée. Le verdict d'une grille est satisfait seulement si tous ses critères
/// obligatoires sont observés : pas de moyenne, pas de compensation, la règle des portes.
/// </summary>
public sealed record HumanReviewGrid(
    string TargetKey,
    string Title,
    string AcceptedEvidence,
    bool IsExplanationComponent,
    int MinimumDurationMinutes,
    IReadOnlyList<HumanReviewCriterion> Criteria);

public static class HumanReviewCatalog
{
    /// <summary>
    /// Cible de la septième grille : la composante Explanation, qui n'est pas une clé de porte.
    /// </summary>
    public const string ExplanationTarget = "component.explanation";

    public static IReadOnlyList<HumanReviewGrid> Grids { get; } = Array.AsReadOnly(
    [
        new HumanReviewGrid(
            MasteryPolicyCatalog.CleanGit,
            "Historique Git propre",
            "Un dépôt accessible au relecteur et une plage de commits nommée, portant du travail réel. Ni capture d'écran, ni export.",
            IsExplanationComponent: false,
            MinimumDurationMinutes: 15,
            Array.AsReadOnly(
            [
                new HumanReviewCriterion(1, "Chaque message de commit dit ce qui change et pourquoi, pas quel fichier est touché", Mandatory: true),
                new HumanReviewCriterion(2, "Aucun commit ne mélange une correction de fond et une reformulation de style", Mandatory: true),
                new HumanReviewCriterion(3, "Un commit isolé pris au hasard dans la plage se comprend sans lire les autres", Mandatory: true),
                new HumanReviewCriterion(4, "Aucun secret, jeton, dump de données ni chemin personnel dans l'historique, y compris supprimé plus tard", Mandatory: true),
                new HumanReviewCriterion(5, "Les branches portent un nom qui dit l'intention ; aucune branche morte non fusionnée sans raison", Mandatory: false),
                new HumanReviewCriterion(6, "Le candidat sait dire pourquoi il a fusionné ou rebasé à un endroit donné", Mandatory: false),
            ])),
        new HumanReviewGrid(
            MasteryPolicyCatalog.TenMinutePresentation,
            "Présentation de 10 minutes",
            "La présentation en direct, ou un enregistrement continu et non monté. Un support sans parole ne suffit pas.",
            IsExplanationComponent: false,
            MinimumDurationMinutes: 8,
            Array.AsReadOnly(
            [
                new HumanReviewCriterion(1, "Le problème est posé avant la solution, et en une phrase", Mandatory: true),
                new HumanReviewCriterion(2, "Au moins une décision technique est justifiée par une contrainte, pas par une préférence", Mandatory: true),
                new HumanReviewCriterion(3, "Une limite connue du travail est énoncée spontanément, sans qu'on la demande", Mandatory: true),
                new HumanReviewCriterion(4, "Le temps est tenu à plus ou moins deux minutes sans couper la fin", Mandatory: true),
                new HumanReviewCriterion(5, "Une question du relecteur reçoit une réponse ou un « je ne sais pas » net, jamais un contournement", Mandatory: true),
                new HumanReviewCriterion(6, "Le support sert le propos et n'est pas lu mot à mot", Mandatory: false),
            ])),
        new HumanReviewGrid(
            MasteryPolicyCatalog.MockInterview,
            "Entretien blanc",
            "Un entretien de 45 à 60 minutes conduit en direct, avec au moins deux relances contradictoires, sur des fiches de content/reference/interviews/ choisies sans être montrées.",
            IsExplanationComponent: false,
            MinimumDurationMinutes: 45,
            Array.AsReadOnly(
            [
                new HumanReviewCriterion(1, "Sur trois questions tirées, les critères observables de la fiche sont énoncés par le candidat, sans être soufflés", Mandatory: true),
                new HumanReviewCriterion(2, "Face à une relance contradictoire, le candidat révise ou tient sa position en donnant une raison ; il ne cède pas au ton", Mandatory: true),
                new HumanReviewCriterion(3, "Une question hors de son domaine reçoit « je ne sais pas, voilà comment je chercherais » plutôt qu'une improvisation", Mandatory: true),
                new HumanReviewCriterion(4, "Aucune affirmation de fait n'est inventée ; le relecteur vérifie au moins une affirmation vérifiable", Mandatory: true),
                new HumanReviewCriterion(5, "Le candidat pose au moins une question qui porte sur le travail réel", Mandatory: false),
            ])),
        new HumanReviewGrid(
            MasteryPolicyCatalog.PragmaticArchitecture,
            "Note d'architecture",
            "Une note écrite de deux à quatre pages sur une décision réellement prise, lue par le relecteur avant tout échange.",
            IsExplanationComponent: false,
            MinimumDurationMinutes: 20,
            Array.AsReadOnly(
            [
                new HumanReviewCriterion(1, "Au moins deux options sont décrites, dont une non retenue mais présentée sous son meilleur jour", Mandatory: true),
                new HumanReviewCriterion(2, "Le critère de départage est nommé et concret : coût d'exploitation, délai, compétence de l'équipe, réversibilité", Mandatory: true),
                new HumanReviewCriterion(3, "Le coût de la décision retenue est énoncé : ce qu'elle rend plus difficile", Mandatory: true),
                new HumanReviewCriterion(4, "Une condition de réexamen est écrite : le fait futur qui invaliderait le choix", Mandatory: true),
                new HumanReviewCriterion(5, "La note tient sans schéma ; les schémas n'ajoutent pas d'information absente du texte", Mandatory: false),
            ])),
        new HumanReviewGrid(
            MasteryPolicyCatalog.English,
            "Anglais professionnel",
            "Deux cartes de content/reference/english/ — une écrite, une orale — traitées devant le relecteur, sans préparation écrite pour la carte orale.",
            IsExplanationComponent: false,
            MinimumDurationMinutes: 20,
            Array.AsReadOnly(
            [
                new HumanReviewCriterion(1, "Les éléments attendus de la carte écrite sont tous présents dans la production du candidat", Mandatory: true),
                new HumanReviewCriterion(2, "Les éléments attendus de la carte orale sont énoncés à l'oral, sans lecture d'un texte préparé", Mandatory: true),
                new HumanReviewCriterion(3, "Aucune des erreurs fréquentes de la carte n'est commise", Mandatory: true),
                new HumanReviewCriterion(4, "Un malentendu provoqué par le relecteur est corrigé et non répété plus fort", Mandatory: true),
                new HumanReviewCriterion(5, "Le registre est professionnel : une objection est formulée sans agressivité ni excuse excessive", Mandatory: false),
            ])),
        new HumanReviewGrid(
            MasteryPolicyCatalog.FinalDefense,
            "Défense du projet final",
            "Une défense en direct de trente minutes minimum, devant au moins deux relecteurs dont un qui n'a pas suivi le projet, code ouvert et navigable.",
            IsExplanationComponent: false,
            MinimumDurationMinutes: 30,
            Array.AsReadOnly(
            [
                new HumanReviewCriterion(1, "Le candidat ouvre le code à un endroit qu'il n'a pas choisi et l'explique", Mandatory: true),
                new HumanReviewCriterion(2, "Pour chaque affirmation de qualité, une preuve est montrée à l'écran, pas racontée", Mandatory: true),
                new HumanReviewCriterion(3, "Ce qui a été fait avec assistance est attribué, spontanément, avant qu'on le demande", Mandatory: true),
                new HumanReviewCriterion(4, "Un défaut trouvé en séance est reconnu sans être minimisé, et son impact est estimé à voix haute", Mandatory: true),
                new HumanReviewCriterion(5, "Le candidat sait dire ce qu'il referait autrement et pourquoi", Mandatory: true),
                new HumanReviewCriterion(6, "Une question sur l'exploitation reçoit une marche à suivre", Mandatory: true),
            ])),
        new HumanReviewGrid(
            ExplanationTarget,
            "Explication d'une solution",
            "Un exercice déjà résolu sans assistance, choisi par le relecteur dans l'historique de pratique, expliqué sans le code sous les yeux.",
            IsExplanationComponent: true,
            MinimumDurationMinutes: 10,
            Array.AsReadOnly(
            [
                new HumanReviewCriterion(1, "L'explication énonce le pourquoi de l'approche, pas la suite des instructions", Mandatory: true),
                new HumanReviewCriterion(2, "Au moins un lien causal est affirmé et vérifiable — « si je retire ceci, tel cas casse » — et le relecteur le vérifie", Mandatory: true),
                new HumanReviewCriterion(3, "Le candidat nomme un cas limite que sa solution traite, et dit comment il l'a su", Mandatory: true),
                new HumanReviewCriterion(4, "Confronté à une entrée qu'il n'a pas vue, il prédit la sortie correctement", Mandatory: true),
                new HumanReviewCriterion(5, "Une approche écartée est mentionnée, avec ce qu'elle coûtait", Mandatory: false),
            ])),
    ]);

    public static HumanReviewGrid? Find(string targetKey) =>
        Grids.FirstOrDefault(grid => string.Equals(grid.TargetKey, targetKey, StringComparison.Ordinal));
}

/// <summary>Un critère rempli par le relecteur : observé ou non, avec ce qui l'a montré.</summary>
public sealed record HumanAttestationCriterionEntry(int Number, bool Observed, string Evidence);

/// <summary>Le contenu d'une attestation avant enregistrement.</summary>
public sealed record HumanAttestationDraft(
    string TargetKey,
    string ReviewerName,
    string ReviewerRelation,
    string LearnerDisplayName,
    DateOnly ReviewedOn,
    int DurationMinutes,
    string ArtifactDescription,
    string NamedGap,
    string? ExplainedExerciseId,
    IReadOnlyList<HumanAttestationCriterionEntry> Criteria);

/// <summary>
/// Règles d'acceptation d'une attestation. Elles tiennent la frontière d'honnêteté du canal : une
/// attestation n'est enregistrée que complète, signée d'un tiers qui n'est pas l'apprenant, sur une
/// grille du protocole, avec tous ses critères obligatoires observés. Tout le reste reste un
/// document personnel de l'apprenant, hors du produit.
/// </summary>
public static partial class HumanAttestationRules
{
    public static IReadOnlyList<string> Validate(HumanAttestationDraft draft)
    {
        var failures = new List<string>();
        HumanReviewGrid? grid = HumanReviewCatalog.Find(draft.TargetKey);
        if (grid is null)
        {
            // Refuser ici toute clé hors protocole est ce qui empêche d'attester une exigence qui
            // possède un producteur automatique : la parole ne remplace jamais une preuve exécutée.
            failures.Add("Cette exigence n'appartient pas au protocole de revue humaine : une attestation n'y est pas admise.");
            return Array.AsReadOnly(failures.ToArray());
        }

        if (string.IsNullOrWhiteSpace(draft.LearnerDisplayName))
        {
            failures.Add("Le profil local ne porte aucun nom : renseignez-le d'abord, le contrôle d'auto-attestation en dépend.");
        }

        if (string.IsNullOrWhiteSpace(draft.ReviewerName))
        {
            failures.Add("Le nom du relecteur est obligatoire : une attestation anonyme n'atteste rien.");
        }
        else if (Normalize(draft.ReviewerName).Length > 0
            && string.Equals(Normalize(draft.ReviewerName), Normalize(draft.LearnerDisplayName), StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Le relecteur ne peut pas être l'apprenant : une auto-attestation est une déclaration, et une déclaration vaut zéro.");
        }

        if (string.IsNullOrWhiteSpace(draft.ReviewerRelation))
        {
            failures.Add("Le lien entre le relecteur et l'apprenant doit être déclaré, même « aucun ».");
        }

        if (draft.ReviewedOn == default)
        {
            failures.Add("La date de la revue est obligatoire.");
        }

        if (draft.DurationMinutes < grid.MinimumDurationMinutes)
        {
            failures.Add($"Cette revue exige au moins {grid.MinimumDurationMinutes} minutes ; une durée inférieure ne peut pas avoir couvert la grille.");
        }

        if (string.IsNullOrWhiteSpace(draft.ArtifactDescription))
        {
            failures.Add("L'artefact examiné doit être nommé : dépôt et plage de commits, note, enregistrement, exercice.");
        }

        if (string.IsNullOrWhiteSpace(draft.NamedGap))
        {
            failures.Add("Un écart nommé est obligatoire, y compris sur un verdict favorable : « c'était bien » n'est pas une revue.");
        }

        if (grid.IsExplanationComponent)
        {
            if (string.IsNullOrWhiteSpace(draft.ExplainedExerciseId) || !ExerciseIdPattern().IsMatch(draft.ExplainedExerciseId))
            {
                failures.Add("La grille d'explication porte sur un exercice précis : son identifiant est obligatoire.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(draft.ExplainedExerciseId))
        {
            failures.Add("Seule la grille d'explication référence un exercice.");
        }

        foreach (HumanReviewCriterion criterion in grid.Criteria)
        {
            HumanAttestationCriterionEntry[] entries = draft.Criteria
                .Where(entry => entry.Number == criterion.Number)
                .ToArray();
            if (entries.Length != 1)
            {
                failures.Add($"Le critère {criterion.Number} doit être rempli exactement une fois : la grille s'applique en entier.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entries[0].Evidence))
            {
                failures.Add($"Le critère {criterion.Number} doit dire ce qui l'a montré, observé ou non : un jugement sans trace ne se relit pas.");
            }

            if (criterion.Mandatory && !entries[0].Observed)
            {
                failures.Add($"Le critère obligatoire {criterion.Number} n'est pas observé : le verdict est non satisfait, et un verdict non satisfait reste un document personnel, hors du produit.");
            }
        }

        if (draft.Criteria.Any(entry => grid.Criteria.All(criterion => criterion.Number != entry.Number)))
        {
            failures.Add("La grille ne contient pas d'autres critères que ceux du protocole.");
        }

        return Array.AsReadOnly(failures.ToArray());
    }

    private static string Normalize(string value) => value.Trim();

    [GeneratedRegex("^[a-z0-9]+(?:[.-][a-z0-9]+)*$")]
    private static partial Regex ExerciseIdPattern();
}
