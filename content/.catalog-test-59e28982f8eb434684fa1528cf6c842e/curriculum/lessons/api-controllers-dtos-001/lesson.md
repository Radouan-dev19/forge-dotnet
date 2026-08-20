# Contrôleurs minces et DTO explicites

## Objectif observable

À la fin de cette leçon, vous saurez écrire un contrôleur qui ne contient aucune règle métier, et
vous saurez énoncer les trois risques concrets qu'on prend en exposant directement une entité
persistée.

## Prérequis

- Avoir lu `api-routing-rest-001` et savoir concevoir un itinéraire de ressource.
- Avoir lu `oop-encapsulation-001` et savoir protéger un invariant.

## Intuition

Un contrôleur est un traducteur. Il transforme une requête HTTP en appel de cas d'usage, puis un
résultat en réponse HTTP. Tout ce qu'il fait d'autre — décider, calculer, valider une règle — sera
introuvable le jour où la même règle devra s'appliquer depuis un traitement par lot.

Le DTO, lui, est la frontière : il définit ce que le monde extérieur peut envoyer et recevoir,
indépendamment de la façon dont vous stockez les choses.

## Explication

**Les trois risques d'exposer une entité.** *Sur-exposition* : l'entité porte des champs internes —
coût d'achat, notes internes, jeton de version — qui partent dans la réponse sans que personne ne
l'ait décidé. Ajouter un champ en base le publie automatiquement.

*Sur-affectation* : à l'entrée, un liant de modèle remplit tous les champs qu'il trouve dans le corps.
Un appelant qui ajoute `"isAdmin": true` ou `"total": 0` modifie ce que vous n'aviez pas prévu
d'exposer. C'est une classe de vulnérabilité réelle, pas une hypothèse.

*Couplage du contrat au stockage* : renommer une colonne devient une rupture d'API. Le schéma de base
et le contrat public évoluent à des rythmes différents ; les lier fige les deux.

**Un DTO par direction.** La requête et la réponse n'ont ni les mêmes champs ni les mêmes règles. Une
requête de création ne porte pas d'identifiant — c'est le serveur qui l'attribue — ni de date de
création, ni de total calculé. Une réponse porte les trois. Utiliser le même type pour les deux
oblige à rendre tout optionnel et supprime la possibilité de valider.

Les `record` conviennent particulièrement : immuables, égalité structurelle, syntaxe courte. Les
propriétés en `init` empêchent la modification après liaison.

**Le contrôleur ne contient que quatre choses.** Recevoir et lier, appeler le cas d'usage, traduire le
résultat en statut, retourner. Aucune boucle métier, aucun calcul de total, aucun accès direct à la
base de données. Le test le plus simple : si supprimer le contrôleur oblige à réécrire une règle,
elle n'était pas au bon endroit.

**La traduction de résultat mérite d'être explicite.** Un cas d'usage retourne un résultat métier —
créé, introuvable, en conflit — et le contrôleur le convertit en statut. Faire remonter des exceptions
comme moyen normal de signaler « introuvable » mélange contrôle de flux et erreur, comme vu dans
`csharp-exceptions-nullable-001`.

**La projection se fait au plus près de la donnée.** Convertir une entité en DTO après l'avoir
entièrement chargée fonctionne, mais rapatrie des colonnes inutiles. Projeter directement dans la
requête — le `Select` vu dans `ef-core-data-access-001` — ne transporte que ce qui sera publié. Sur
une liste, la différence est immédiate.

**Nommer les champs pour l'appelant.** Le DTO est lu par quelqu'un qui ne connaît pas votre schéma.
`customerName` vaut mieux que `custNm`, et une énumération publiée sous forme de texte
(`"status": "Paid"`) vaut mieux qu'un entier dont la signification vit dans votre code. Un entier
publié devient un contrat que vous ne pourrez plus réordonner.

## Exemple commenté

Deux DTO distincts, un par direction :

```csharp
// Entrée : pas d'identifiant, pas de total, pas de statut — le serveur les décide.
public sealed record CreateOrderRequest(int CustomerId, IReadOnlyList<CreateOrderLine> Lines);

public sealed record CreateOrderLine(int ProductId, int Quantity);

// Sortie : ce que l'appelant a le droit de voir, et rien d'autre.
// Le statut est publié en texte pour ne pas figer un entier dans le contrat.
public sealed record OrderResponse(int OrderId, string CustomerName, string Status, decimal Total);
```

Le contrôleur correspondant, réduit à sa fonction de traduction :

```csharp
[HttpPost("/orders")]
public async Task<IActionResult> CreateAsync(
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    // Aucune règle ici : le cas d'usage décide, le contrôleur traduit.
    CreateOrderOutcome outcome = await _createOrder.ExecuteAsync(request, cancellationToken);

    return outcome.Kind switch
    {
        CreateOrderKind.Created => Created($"/orders/{outcome.OrderId}", Map(outcome.Order!)),
        CreateOrderKind.CustomerUnknown => NotFound(),
        CreateOrderKind.InsufficientStock => Conflict(),
        _ => throw new InvalidOperationException("Résultat de création non traité."),
    };
}

private static OrderResponse Map(Order order) =>
    new(order.Id, order.Customer.Name, order.Status.ToString(), order.Total);
```

Et la projection au plus près de la donnée, pour une liste :

```csharp
// Une seule requête, seules les colonnes publiées traversent le réseau,
// et le résultat n'est pas une entité, donc jamais suivi.
public Task<List<OrderResponse>> ListAsync(string city, CancellationToken cancellationToken) =>
    _context.Orders
        .Where(order => order.Customer.City == city)
        .OrderByDescending(order => order.Total).ThenBy(order => order.Id)
        .Select(order => new OrderResponse(
            order.Id, order.Customer.Name, order.Status, order.Total))
        .Take(50)
        .ToListAsync(cancellationToken);
```

## Contre-exemple et erreur fréquente

```csharp
[HttpPost("/orders")]
public IActionResult Create([FromBody] Order order)      // L'entité persistée sert de contrat d'entrée.
{
    // Règle métier dans le contrôleur : introuvable depuis un traitement par lot.
    decimal total = 0m;
    foreach (OrderLine line in order.Lines)
    {
        total += line.Quantity * line.UnitPrice;
    }

    order.Total = total;
    order.Status = "Open";
    _context.Orders.Add(order);
    _context.SaveChanges();

    return Ok(order);                                     // L'entité entière repart en réponse.
}
```

Quatre défauts qui se renforcent.

`[FromBody] Order` ouvre la sur-affectation : un appelant peut envoyer `"id": 7`, `"total": 0` ou
`"status": "Paid"`, et le liant les affectera. La ligne `order.Total = total` corrige l'un des trois
par accident, pas les autres.

Le calcul du total vit dans le contrôleur. Le jour où une commande est créée par un import nocturne,
soit la règle est dupliquée, soit les totaux divergent.

`return Ok(order)` renvoie l'entité entière, avec ses propriétés de navigation, ses champs internes et
son jeton de version. Ajouter demain une colonne « coût d'achat » la publiera sans que personne ne le
décide.

Enfin, l'accès direct au contexte dans le contrôleur rend la règle intestable sans base de données, ce
qui est exactement ce que `tests-api-factory-001` cherche à éviter.

## Vérification de compréhension

Nommez les trois risques d'exposer une entité persistée, et dites lequel est le plus difficile à
détecter en revue de code.

:::quiz
id=api-controllers-dtos-001-check
question=Pourquoi utiliser un type distinct pour la requête et pour la réponse plutôt qu'un seul type partagé ?
option=Parce que le sérialiseur refuse d'utiliser le même type dans les deux sens
option=Parce que les deux directions n'ont ni les mêmes champs ni les mêmes règles : partager force à tout rendre optionnel et supprime la validation
option=Parce qu'un type partagé consomme deux fois plus de mémoire à la sérialisation
correct=1
success=Correct : une requête de création ne porte ni identifiant, ni total calculé, ni date de création. Les mutualiser rend ces champs optionnels et ouvre la sur-affectation.
retry=Relisez le passage sur un DTO par direction, et demandez-vous ce que devient la validation quand un champ doit être absent à l'entrée mais présent en sortie.
:::

## Exercice guidé

Ouvrez `api-dto-customer-name-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que le DTO accepte et ce qu'il refuse, y compris la valeur absente.
2. Implémentez la normalisation, en distinguant absence et chaîne de blancs.
3. Vérifiez que le repli retourné ne divulgue rien de l'état interne.
4. Lisez ensuite `content/labs/api-mini-erp/src/ForgeApiLab/Models/OrderContracts.cs` pour voir un jeu
   de contrats complet.

## Exercice autonome

Concevez les DTO d'entrée et de sortie d'une opération « créer un avoir sur une facture ».

Décidez avant d'écrire : les champs de chaque direction, ceux que le serveur attribue, la façon dont
vous publiez un statut, ce que vous faites d'un champ interne qui ne doit jamais sortir, et où vous
placez le calcul du montant restant dû.

## Débogage

Un ticket indique : « Un client a réussi à créer une commande déjà marquée comme payée. »

1. **Symptôme** : un champ que l'API n'est pas censée accepter a bien été pris en compte.
2. **Hypothèse** : l'entité sert de contrat d'entrée et le liant remplit tous ses champs.
3. **Preuve** : envoyez une requête contenant un champ non documenté et observez l'état persisté. S'il
   change, la sur-affectation est confirmée.
4. **Prévention** : introduire un DTO d'entrée ne portant que les champs autorisés, et ajouter un test
   qui envoie un champ interdit et vérifie qu'il est ignoré.

## Entretien

Question posée à voix haute : *pourquoi ne pas exposer directement vos entités dans une API ?*

Une réponse solide cite les trois risques — sur-exposition, sur-affectation, couplage au stockage — et
donne un exemple vécu d'au moins l'un d'eux. Elle reconnaît aussi le coût du DTO : du code de
correspondance à maintenir, justifié par la stabilité du contrat.

## Résumé

- Un contrôleur reçoit, appelle, traduit, retourne — rien d'autre.
- Exposer une entité crée trois risques distincts, dont la sur-affectation, silencieuse.
- Un DTO par direction : les champs et les règles diffèrent.
- Publier une énumération en texte évite de figer un entier dans le contrat.
- Projeter dans la requête ne transporte que ce qui sera publié.

## Cartes de révision

Question : quel test simple révèle qu'une règle est au mauvais endroit ? Réponse attendue : supprimer
le contrôleur obligerait à la réécrire ailleurs.

Question : que risque-t-on à publier un statut sous forme d'entier ? Réponse attendue : la valeur
numérique devient un contrat public qu'on ne peut plus réordonner.

## Test de maîtrise

Sans relire, écrivez les DTO et le contrôleur d'une opération « enregistrer un règlement ».
Justifiez la séparation entrée/sortie, montrez où se situe la règle métier, indiquez les statuts
retournés pour chaque issue, et expliquez comment vous empêchez qu'un champ interne parte en réponse.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
