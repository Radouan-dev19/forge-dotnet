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

    /// <summary>
    /// Guide de carrière de la semaine 24 : CV par preuves, entretien, prospection, négociation,
    /// prise de poste.
    /// </summary>
    /// <remarks>
    /// Ces guides existaient sous <c>content/reference/career</c> sans être un type de document :
    /// ni chargés, ni validés, ni servis par aucune route — l'angle mort que la règle de
    /// joignabilité ne pouvait pas voir, faute d'entrée dans cette énumération. Chaque guide porte
    /// un manifeste plat qui référence son Markdown, ce qui place sa prose sous les règles
    /// d'authenticité du validateur. Un guide se lit et s'applique hors du produit : il ne produit
    /// aucune preuve de maîtrise, et chaque page l'annonce.
    /// </remarks>
    CareerGuide,

    /// <summary>
    /// Guide du chapitre IA : utiliser un assistant de code en professionnel — économie de tokens,
    /// paramétrage, skills, agents et sous-agents, boucle de travail quotidienne.
    /// </summary>
    /// <remarks>
    /// Chapitre volontairement <b>hors parcours</b> : aucun prérequis, aucune semaine, aucun ordre
    /// imposé au-delà d'une suggestion de lecture — il se consulte selon les besoins. Aucun bac à
    /// sable ne peut vérifier l'usage d'un assistant (le runner n'a pas de réseau, par conception) :
    /// ces guides ne produisent aucune preuve de maîtrise et chaque page l'annonce. Ils tiennent la
    /// ligne du contrat d'apprentissage : l'IA est un outil de métier à apprendre, jamais un moyen
    /// de produire les preuves comptées du parcours.
    /// </remarks>
    AiGuide,
}
