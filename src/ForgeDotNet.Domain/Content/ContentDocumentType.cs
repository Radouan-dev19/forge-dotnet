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

    /// <summary>
    /// Guide du chapitre Cloud : modèle mental du cloud, panorama Azure, des conteneurs à
    /// l'orchestration, Kubernetes, et sécurité/coûts.
    /// </summary>
    /// <remarks>
    /// Chapitre volontairement <b>hors parcours</b>, comme le chapitre IA : aucun prérequis, aucune
    /// semaine, aucun ordre imposé au-delà d'une suggestion de lecture. Le bloc Azure noté (semaines
    /// 21-22) reste distinct et continue de produire ses preuves de maîtrise par les exercices
    /// <c>azure-*</c> ; ces guides ne le remplacent pas, ils apportent la vue d'ensemble et couvrent
    /// Kubernetes, absent du parcours noté. Aucun bac à sable ne peut vérifier l'usage réel d'un
    /// cloud ou d'un cluster : ces guides ne produisent aucune preuve de maîtrise et chaque page
    /// l'annonce.
    /// </remarks>
    CloudGuide,

    /// <summary>
    /// Guide de la Semaine 0 : notions d'appoint à connaître avant ou en marge du parcours —
    /// Vue.js 3/TypeScript, Azure Service Bus, Azure DevOps, IIS, Microservices.
    /// </summary>
    /// <remarks>
    /// Onglet « Semaine 0 » de la page Apprendre, volontairement <b>hors parcours</b> : ni
    /// semaine numérotée dans <c>forge-reference</c>, ni prérequis, ni preuve de maîtrise. Le
    /// bac à sable ne peut vérifier ni un projet front-end réel ni un service Azure réel : ces
    /// guides ne produisent donc aucune observation de maîtrise, comme les chapitres IA et
    /// Cloud.
    /// </remarks>
    WeekZeroGuide,

    /// <summary>
    /// Dossier de préparation à un entretien précis, groupé par sous-thème (une entreprise, un
    /// poste) : plan de travail, liens vers les cours de la plateforme, scripts de réponse.
    /// </summary>
    /// <remarks>
    /// Onglet « Entretiens ciblés », volontairement <b>hors parcours</b> : ni semaine, ni prérequis,
    /// ni preuve de maîtrise. Un dossier référence les leçons et guides déjà publiés plutôt que de
    /// les recopier — la règle anti-recopie du validateur s'applique à lui comme au reste — et ne
    /// contient aucune donnée personnelle : pas de nom, pas de contact, pas d'employeur actuel.
    /// </remarks>
    PrepGuide,

    /// <summary>
    /// Banque de quiz théorique de niveau senior : questions à choix multiples, une ou plusieurs
    /// réponses justes par question, réponse vérifiée dans la session puis oubliée.
    /// </summary>
    /// <remarks>
    /// Onglet « Thib », volontairement <b>hors parcours</b> : ni semaine, ni prérequis, ni preuve de
    /// maîtrise. Une banque ressemble à une banque de cartes de révision, mais elle ne se rattache à
    /// aucun exercice et n'alimente aucune file : elle sonde la théorie qu'un profil senior doit
    /// tenir sans hésiter — sémantique du runtime, asynchronisme, données, plateforme web. Le
    /// corrigé d'une question n'est jamais rendu avant la vérification, et rien n'est enregistré.
    /// </remarks>
    TheoryQuizBank,
}
