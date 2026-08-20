# Pagination, filtrage et tri bornés

## Objectif observable

À la fin de cette leçon, vous saurez borner une taille de page côté serveur, n'accepter un critère de
tri que depuis une liste blanche, et expliquer pourquoi un tri non total fait apparaître deux fois la
même ligne à des pages différentes.

## Prérequis

- Avoir lu `api-async-cancellation-001` et savoir borner le coût d'une requête dans le temps.
- Avoir lu `ef-core-data-access-001` et savoir ce qu'une requête exécute réellement en base.

## Intuition

Une collection sans pagination fonctionne parfaitement jusqu'au jour où elle contient un million de
lignes. Ce jour-là, un seul appel suffit à saturer la mémoire du serveur et à immobiliser la base.
Toute collection exposée est donc paginée **par défaut**, pas sur demande.

Le second réflexe : tout paramètre de liste vient du client, et donc n'est pas digne de confiance.
Taille de page, décalage, nom de colonne de tri, terme de recherche — chacun doit être borné ou
validé.

## Explication

**La taille de page se borne côté serveur, toujours.** Le client propose, le serveur dispose. Une
valeur absente prend un défaut raisonnable, une valeur trop grande est ramenée au maximum, une valeur
nulle ou négative est refusée ou ramenée au défaut. Ce maximum est un réglage de configuration, pas un
nombre écrit dans le code — c'est ce que montre `api-configuration-secrets-errors-001`.

Sans ce plafond, `?pageSize=1000000` est une attaque en déni de service qui tient dans une barre
d'adresse.

**Le tri doit être total, sinon la pagination ment.** Trier par ville seule sur des données où trois
cents lignes partagent la même ville laisse la base libre de renvoyer ces lignes dans n'importe quel
ordre — et cet ordre peut différer entre deux requêtes. Une ligne apparaît alors en page 2 et de
nouveau en page 3, tandis qu'une autre n'apparaît jamais.

La correction est mécanique : **ajouter toujours un départage unique** en dernier critère, en général
la clé primaire. Le tri devient déterministe, et la pagination redevient honnête.

**Le critère de tri se valide contre une liste blanche.** Concaténer un nom de colonne reçu du client
dans une requête est une injection, exactement comme concaténer une valeur. Même avec une requête
paramétrée, le nom de colonne ne peut pas être un paramètre : il doit être choisi dans un ensemble
fermé, défini dans le code.

La liste blanche rend aussi le contrat explicite : le document de contrat peut énumérer les tris
possibles, ce qu'un tri libre ne permettrait jamais.

**Décalage ou curseur.** Le décalage — sauter *n* lignes — est simple et permet d'aller directement à
une page. Son coût croît avec la profondeur : la base doit produire puis jeter les lignes sautées, et
la page 10 000 est très lente. Il souffre aussi du décalage de contenu : une insertion entre deux
requêtes décale toutes les pages suivantes.

Le curseur — « ce qui vient après cette clé » — a un coût constant et ne souffre pas de l'insertion,
mais interdit le saut direct à une page arbitraire. Le critère : pagination d'interface avec numéros
de page, décalage ; parcours complet d'un jeu volumineux ou flux, curseur.

**Le filtrage se borne aussi.** Un terme de recherche est normalisé — blancs retirés, casse ignorée —
et une longueur minimale évite de balayer la table entière sur une seule lettre. Une recherche
commençant par un joker empêche l'usage de l'index et transforme chaque appel en balayage complet.

**La réponse porte de quoi naviguer.** Total, page courante, taille effectivement appliquée. La taille
retournée est celle qui a été **appliquée**, pas celle qui a été demandée : c'est ainsi que le client
apprend que sa demande a été bornée. Le total, en revanche, coûte une seconde requête d'agrégat ; sur
de gros volumes il est parfois préférable de ne renvoyer que l'existence d'une page suivante.

## Exemple commenté

Les trois bornes, chacune isolée et testable :

```csharp
public static int NormalizePageSize(int requested, int defaultSize, int maximumSize)
{
    // Une demande absente ou absurde retombe sur le défaut ; une demande
    // excessive est ramenée au plafond au lieu d'être refusée : le client
    // obtient un résultat, borné, plutôt qu'une erreur.
    if (requested <= 0)
    {
        return defaultSize;
    }

    return Math.Min(requested, maximumSize);
}

public static int SkipCount(int pageNumber, int pageSize)
{
    // La première page ne saute rien. Un numéro invalide n'est pas silencieusement
    // corrigé en interne : il est ramené à la première page, seule valeur sûre.
    int safePage = Math.Max(1, pageNumber);
    return (safePage - 1) * pageSize;
}
```

La liste blanche de tri, seul moyen sûr d'accepter un nom de colonne :

```csharp
// Ensemble fermé : ce qui n'y figure pas ne peut pas atteindre la requête.
private static readonly Dictionary<string, Expression<Func<Order, object>>> SortKeys =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["total"] = order => order.Total,
        ["date"] = order => order.CreatedOn,
        ["customer"] = order => order.Customer.Name,
    };

public static string NormalizeSort(string? requested, string fallback) =>
    // Un critère inconnu ne provoque pas d'erreur : il retombe sur le tri par défaut.
    requested is not null && SortKeys.ContainsKey(requested) ? requested.ToLowerInvariant() : fallback;
```

Et la requête complète, avec le départage qui rend le tri total :

```csharp
public async Task<PagedResult<OrderResponse>> ListAsync(
    OrderQuery query,
    CancellationToken cancellationToken)
{
    int pageSize = NormalizePageSize(query.PageSize, _options.DefaultPageSize, _options.MaximumPageSize);

    IQueryable<Order> filtered = _context.Orders;
    if (!string.IsNullOrWhiteSpace(query.Term))
    {
        string term = query.Term.Trim();
        filtered = filtered.Where(order => order.Customer.Name.Contains(term));
    }

    // ThenBy sur la clé primaire : sans lui, deux lignes de même total peuvent
    // changer d'ordre entre deux requêtes et apparaître deux fois, ou jamais.
    IOrderedQueryable<Order> ordered = filtered
        .OrderByDescending(SortKeys[NormalizeSort(query.Sort, "date")])
        .ThenBy(order => order.Id);

    int total = await ordered.CountAsync(cancellationToken);
    List<OrderResponse> items = await ordered
        .Skip(SkipCount(query.Page, pageSize))
        .Take(pageSize)
        .Select(order => new OrderResponse(order.Id, order.Customer.Name, order.Status, order.Total))
        .ToListAsync(cancellationToken);

    // pageSize retourné est celui appliqué : le client voit que sa demande a été bornée.
    return new PagedResult<OrderResponse>(items, total, Math.Max(1, query.Page), pageSize);
}
```

## Contre-exemple et erreur fréquente

```csharp
[HttpGet("/orders")]
public IActionResult List(int pageSize, int page, string sort, string term)
{
    // Aucune borne : pageSize=1000000 charge tout en mémoire.
    // Nom de colonne concaténé : injection par le critère de tri.
    string sql = $"SELECT * FROM Orders WHERE CustomerName LIKE '%{term}%' ORDER BY {sort}";

    List<Order> all = _context.Orders.FromSqlRaw(sql).ToList();

    // Pagination effectuée en mémoire, après avoir tout rapatrié :
    // le coût du chargement complet a déjà été payé.
    return Ok(all.Skip(page * pageSize).Take(pageSize));
}
```

Cinq défauts, dont deux critiques.

`sort` est concaténé dans la requête. Un appelant peut y écrire autre chose qu'un nom de colonne : la
requête paramétrée ne protège pas ici, parce qu'un nom de colonne ne peut pas être un paramètre. Seule
la liste blanche protège.

`term` est concaténé de la même façon, ce qui ajoute une seconde injection, et le joker de tête
interdit l'usage de tout index : chaque appel devient un balayage complet.

`pageSize` sans plafond permet de saturer la mémoire du serveur avec une seule URL.

La pagination en mémoire arrive après `ToList()` : les millions de lignes ont déjà traversé le réseau
et été matérialisées. Il fallait paginer dans la requête.

Enfin, `page * pageSize` saute une page de trop : avec `page = 1`, la première page est perdue. Le
décalage se calcule sur un numéro commençant à un.

## Vérification de compréhension

Une liste triée par montant décroissant affiche la même commande en page 2 et en page 3. Expliquez la
cause exacte et donnez la correction en une ligne de code.

:::quiz
id=api-pagination-filtering-sorting-001-check
question=Pourquoi un critère de tri reçu du client doit-il être choisi dans une liste fermée plutôt que passé en paramètre de requête ?
option=Parce qu'un nom de colonne ne peut pas être un paramètre : il est concaténé dans la requête, et seule une liste fermée empêche l'injection
option=Parce que le tri par paramètre est plus lent que le tri par liste fermée
option=Parce que la base de données refuse les noms de colonne reçus en paramètre
correct=0
success=Correct : la paramétrisation protège les valeurs, pas les identifiants. Un nom de colonne ne peut être sûr que s'il provient d'un ensemble défini dans le code.
retry=Relisez le passage sur la liste blanche de tri, et demandez-vous ce qu'une requête paramétrée protège exactement.
:::

## Exercice guidé

Ouvrez `api-page-size-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, le comportement attendu pour une taille nulle, négative, raisonnable et
   excessive.
2. Implémentez la borne en distinguant le repli sur le défaut et l'écrêtage au plafond.
3. Vérifiez les deux valeurs de bordure : exactement le plafond, et le plafond plus un.
4. Enchaînez avec `api-skip-count-001`, `api-sort-whitelist-001` puis `api-filter-term-001` pour
   couvrir les trois autres bornes.

## Exercice autonome

Concevez le point d'entrée de liste d'une ressource « facture » avec filtres par statut, par période
et par client.

Décidez avant d'écrire : les valeurs par défaut, le plafond de page et son origine, les critères de
tri autorisés, le départage garantissant un ordre total, ce que vous renvoyez pour un critère inconnu,
et si vous calculez le total ou seulement l'existence d'une page suivante.

## Débogage

Un ticket indique : « L'export en plusieurs pages contient des doublons et il manque des lignes. »

1. **Symptôme** : des lignes apparaissent deux fois, d'autres jamais, sans régularité.
2. **Hypothèse** : le tri n'est pas total, et l'ordre entre lignes égales varie d'une requête à
   l'autre.
3. **Preuve** : comparez le critère de tri au nombre de valeurs distinctes qu'il produit. Un critère à
   faible cardinalité sur un jeu volumineux confirme.
4. **Prévention** : ajouter un départage unique en dernier critère, et ajouter un test qui parcourt
   toutes les pages et vérifie l'absence de doublon.

## Entretien

Question posée à voix haute : *comment exposez-vous une collection potentiellement très volumineuse ?*

Une réponse solide impose la pagination par défaut, borne la taille côté serveur, cite le tri total
comme condition de correction, valide le critère de tri contre une liste fermée, et sait arbitrer
entre décalage et curseur selon l'usage.

## Résumé

- Toute collection exposée est paginée par défaut, jamais sur demande.
- La taille de page est bornée côté serveur, depuis un réglage de configuration.
- Sans départage unique, la pagination produit doublons et oublis.
- Un nom de colonne ne se paramètre pas : il se choisit dans une liste fermée.
- La réponse annonce la taille appliquée, pas celle demandée.

## Cartes de révision

Question : pourquoi le décalage devient-il lent en profondeur ? Réponse attendue : la base doit
produire puis jeter toutes les lignes sautées.

Question : que casse une recherche commençant par un joker ? Réponse attendue : l'usage de l'index,
donc chaque appel devient un balayage complet.

## Test de maîtrise

Sans relire, écrivez le point d'entrée de liste d'une ressource de votre choix : signature complète,
bornes appliquées et leur origine, liste des tris autorisés, requête produite avec son ordre total,
forme de la réponse, et les deux tests qui prouvent qu'aucune page ne peut dépasser le plafond ni
contenir de doublon.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
