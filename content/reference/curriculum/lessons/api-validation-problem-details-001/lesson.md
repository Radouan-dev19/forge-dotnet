# Validation et erreurs Problem Details

## Objectif observable

À la fin de cette leçon, vous saurez séparer la validation de forme de la règle métier, retourner une
erreur au format normalisé qui permet à l'appelant de corriger sa requête, et démontrer qu'aucune
information interne ne fuit par le corps d'erreur.

## Prérequis

- Avoir lu `api-controllers-dtos-001` et savoir borner un contrat d'entrée.
- Avoir lu `csharp-exceptions-nullable-001` et savoir distinguer absence attendue et violation.

## Intuition

Une requête traverse trois filtres successifs. *La forme* : le corps est-il du JSON valide, les types
correspondent-ils ? *La structure* : les champs obligatoires sont-ils là, les bornes respectées ?
*Le métier* : cette commande est-elle possible compte tenu du stock, du statut, des droits ?

Chaque filtre rejette pour une raison différente et mérite une réponse différente. Les mélanger produit
des messages que personne ne peut exploiter.

## Explication

**La validation de forme est gratuite et doit venir en premier.** Elle ne demande aucun accès à la
base et rejette les requêtes absurdes avant qu'elles ne coûtent quoi que ce soit. Les annotations de
données couvrent l'essentiel : obligatoire, longueur, intervalle, motif. Une validation par
composant dédié convient quand la règle dépend de plusieurs champs.

**La règle métier est différente par nature.** « Le stock est insuffisant » n'est pas une erreur de
saisie : la requête est parfaitement formée. Elle mérite un statut distinct — `409` pour un conflit
avec l'état courant, `422` pour un contenu métier inacceptable — et surtout elle vit dans le domaine,
pas dans un attribut posé sur un DTO.

Le critère : si la règle doit tenir quel que soit l'appelant, y compris un traitement par lot, elle
appartient au domaine. C'est le même raisonnement que pour les contraintes de base de données vu dans
`sql-relational-constraints-001`.

**Une forme d'erreur normalisée, une seule.** Le format Problem Details du web définit un objet avec
`type`, `title`, `status`, `detail` et `instance`, extensible par des champs propres. Son intérêt
n'est pas esthétique : un client peut écrire **un seul** code de traitement d'erreur au lieu d'un par
point d'entrée.

Pour les erreurs de validation, l'extension attendue est un dictionnaire associant chaque champ à ses
messages. C'est ce qui permet à une interface d'afficher l'erreur en face du bon champ, plutôt qu'un
bandeau générique.

**`detail` s'adresse à l'appelant, pas à vous.** « La quantité doit être comprise entre 1 et 100 »
est actionnable. « Object reference not set to an instance of an object » ne l'est pas, et divulgue
que vous avez un défaut. La règle est absolue : **aucune trace d'exception, aucun nom de table, aucune
requête, aucun chemin de fichier dans une réponse**.

**L'identifiant de corrélation est le pont.** Une réponse d'erreur porte un identifiant que l'on
retrouve dans les journaux serveur. L'appelant le communique au support, et le diagnostic devient
possible sans avoir rien divulgué. C'est le mécanisme développé dans `observability-correlation-001`.

**Centraliser plutôt que répéter.** Un intergiciel de gestion d'erreurs attrape ce qui remonte,
journalise avec la pile **côté serveur**, et retourne une réponse normalisée sans détail interne.
Sans lui, chaque point d'entrée réinvente son format, et il suffit d'un oubli pour qu'une trace
parte en clair.

**Ne jamais faire confiance à ce qui vient du client, y compris à ce qui semble inoffensif.** Une
taille de page, un nom de tri, un identifiant : tout doit être borné ou validé contre une liste
blanche. Ce point est repris dans `api-pagination-filtering-sorting-001` et
`security-owasp-api-001`.

## Exemple commenté

Le DTO porte la validation de forme ; le domaine porte la règle :

```csharp
public sealed record CreateOrderRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant client doit être strictement positif.")]
    public int CustomerId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Une commande comporte au moins une ligne.")]
    [MaxLength(100, ErrorMessage = "Une commande ne peut pas dépasser cent lignes.")]
    public IReadOnlyList<CreateOrderLine> Lines { get; init; } = [];
}
```

La réponse normalisée que reçoit l'appelant :

```json
{
  "type": "https://forge.local/problems/validation",
  "title": "Requête invalide.",
  "status": 400,
  "detail": "Un ou plusieurs champs ne respectent pas le contrat.",
  "instance": "/orders",
  "traceId": "b7c1e2f4a9",
  "errors": {
    "customerId": ["L'identifiant client doit être strictement positif."],
    "lines":      ["Une commande comporte au moins une ligne."]
  }
}
```

Le champ `errors` permet un affichage en face du bon champ. `traceId` relie la réponse au journal
serveur sans rien divulguer.

Et l'intergiciel qui garantit qu'aucun détail interne ne sort, quelle que soit l'exception :

```csharp
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    try
    {
        await next(context);
    }
    catch (Exception exception)
    {
        // La pile complète va au journal, côté serveur, avec l'identifiant de corrélation.
        _logger.LogError(exception, "Échec non traité {TraceId}", context.TraceIdentifier);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://forge.local/problems/internal",
            Title = "Erreur interne.",
            Status = StatusCodes.Status500InternalServerError,
            // Aucun message d'exception : l'appelant reçoit de quoi nous contacter, rien de plus.
            Detail = "L'opération a échoué. Communiquez l'identifiant de trace au support.",
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier },
        });
    }
}
```

## Contre-exemple et erreur fréquente

```csharp
[HttpPost("/orders")]
public IActionResult Create(CreateOrderRequest request)
{
    try
    {
        // Validation de forme et règle métier mélangées, chacune avec son propre format.
        if (request.CustomerId <= 0)
        {
            return BadRequest("customerId invalide");
        }

        Order order = _createOrder.Execute(request);
        return Ok(order);
    }
    catch (Exception exception)
    {
        // La trace complète part au client : schéma, chemins, versions de bibliothèques.
        return StatusCode(500, new { error = exception.ToString() });
    }
}
```

Trois défauts, du plus visible au plus grave.

Le format d'erreur est propre à ce point d'entrée : une chaîne ici, un objet ailleurs. Chaque client
doit écrire un traitement différent par opération.

La validation de forme est écrite à la main dans le contrôleur, donc absente du document de contrat et
non répétée sur les autres points d'entrée. Le jour où une seconde opération accepte un
`customerId`, la règle sera oubliée.

`exception.ToString()` dans la réponse est le défaut critique. Il divulgue les espaces de noms
internes, les chemins de fichiers du serveur de build, parfois une requête et le nom des colonnes.
C'est une aide directe à qui cherche une faille — et c'est aussi ce qui transforme un incident mineur
en incident de sécurité.

La correction tient en trois gestes : annotations sur le DTO, règle métier dans le domaine avec un
statut distinct, et intergiciel central pour la forme des erreurs.

## Vérification de compréhension

Pour « la quantité demandée dépasse le stock », dites s'il s'agit d'une validation de forme ou d'une
règle métier, quel statut vous retournez, et où vit la règle.

:::quiz
id=api-validation-problem-details-001-check
question=Que doit contenir le champ `detail` d'une réponse d'erreur de production ?
option=Le message de l'exception, pour que le client puisse transmettre l'information exacte au support
option=Une explication actionnable pour l'appelant, accompagnée d'un identifiant de corrélation, sans aucun détail interne
option=La requête reçue, afin que l'appelant vérifie ce qu'il a envoyé
correct=1
success=Correct : la pile va au journal serveur, l'appelant reçoit de quoi corriger sa requête et un identifiant qui permet le diagnostic sans divulgation.
retry=Relisez le passage sur ce que `detail` doit contenir, et le rôle de l'identifiant de corrélation.
:::

## Exercice guidé

Ouvrez `api-order-validation-001` dans `/practice`, puis procédez ainsi.

1. Classez, avant tout code, chaque cas en absence de valeur, valeur hors intervalle, ou valeur
   acceptable.
2. Implémentez la décision en distinguant explicitement les trois issues.
3. Vérifiez les bornes exactes de l'intervalle, dans les deux sens.
4. Ouvrez ensuite `api-error-status-001` pour relier une catégorie d'erreur à son statut.

## Exercice autonome

Concevez la validation complète d'une requête « créer un règlement » : identifiant de facture,
montant, mode de paiement, date de valeur.

Décidez avant d'écrire : ce qui relève de la forme et ce qui relève du métier, le statut retourné par
chaque famille, la forme exacte du corps d'erreur, et ce que vous répondez si le montant dépasse le
reste dû.

## Débogage

Un ticket indique : « Un utilisateur a envoyé une capture d'écran contenant le nom de nos tables. »

1. **Symptôme** : de l'information interne est visible côté client.
2. **Hypothèse** : une réponse d'erreur contient un message d'exception non filtré.
3. **Preuve** : provoquez l'erreur signalée et inspectez le corps renvoyé. La présence d'un nom
   d'espace de noms ou de table confirme.
4. **Prévention** : centraliser la gestion d'erreurs dans un intergiciel, et ajouter un test qui
   provoque une exception et vérifie que la réponse ne contient ni pile, ni chemin, ni nom de table.

## Entretien

Question posée à voix haute : *où placez-vous la validation dans une API, et pourquoi à cet endroit ?*

Une réponse solide distingue les trois filtres, explique que la forme se valide au plus tôt parce
qu'elle est gratuite, et que la règle métier vit dans le domaine pour tenir quel que soit l'appelant.
Elle mentionne le format d'erreur normalisé comme service rendu aux clients.

## Résumé

- Forme, structure, métier : trois filtres, trois réponses.
- Une règle qui doit tenir pour tout appelant appartient au domaine.
- Un format d'erreur unique permet un seul traitement côté client.
- `detail` sert à corriger la requête, jamais à décrire l'infrastructure.
- L'identifiant de corrélation relie la réponse au journal sans divulguer.

## Cartes de révision

Question : quel statut distingue une requête mal formée d'une requête valide mais impossible ?
Réponse attendue : `400` pour la forme, `409` ou `422` selon qu'il s'agit d'un conflit d'état ou d'un
contenu métier inacceptable.

Question : pourquoi centraliser la gestion des erreurs dans un intergiciel ? Réponse attendue : il
suffit d'un point d'entrée oublié pour qu'une trace parte en clair.

## Test de maîtrise

Sans relire, décrivez le traitement complet d'une requête invalide dans une API : les trois filtres, le
statut de chacun, la forme exacte du corps retourné, ce qui va au journal, et le test qui prouve
qu'aucune information interne ne sort.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
