namespace ForgeDotNet.Domain.Content;

public enum ContentDocumentType
{
    Lesson,
    Exercise,
    Curriculum,
    DebugScenario,
    SqlScenario,
    InterviewQuestion,
    EnglishActivity,
    Project,

    /// <summary>
    /// Banque des cartes de révision à choix rattachées aux exercices.
    /// </summary>
    ReviewCardBank,

    /// <summary>
    /// Laboratoire exécuté par l'apprenant sur son poste, hors du bac à sable.
    /// </summary>
    /// <remarks>
    /// Un laboratoire est le seul contenu qui porte un vrai projet compilable — fichier de projet,
    /// conteneur durci, définition d'infrastructure, chaîne de livraison. Il vit sous
    /// <c>content/labs</c> et non sous <c>content/reference</c>, donc dans un catalogue distinct, pour
    /// la même raison que les scénarios SQL : le lecteur ne doit pas charger des arborescences de code
    /// qui ne se lisent pas comme une leçon.
    ///
    /// Sa réussite est <b>déclarée</b> par l'apprenant et ne produit aucune preuve de maîtrise :
    /// l'exécution a lieu chez lui, hors de toute instrumentation du serveur. Le schéma l'impose par
    /// une valeur constante, de sorte qu'un manifeste ne peut pas prétendre le contraire.
    /// </remarks>
    Lab,
}
