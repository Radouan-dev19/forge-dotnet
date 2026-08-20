# EF Core : tracking, chargement et concurrence

## Objectif observable

À la fin de cette leçon, vous saurez dire quelle requête EF Core part réellement en base et laquelle
est évaluée en mémoire, supprimer un N+1 en choisissant entre inclusion et projection, et gérer un
conflit de concurrence par jeton de version.

## Prérequis

- Avoir lu `sql-pagination-001` et savoir écrire une pagination bornée.
- Avoir lu `linq-lambdas-001` et savoir raisonner sur l'exécution différée.

## Intuition

EF Core traduit du LINQ en SQL. Tout le reste découle de cette phrase : ce qui est traduisible part en
base, ce qui ne l'est pas doit être exécuté en mémoire — après avoir rapatrié les données.

Le second mécanisme à comprendre est le suivi de modifications. Le contexte mémorise l'état initial de
chaque entité chargée pour pouvoir calculer les instructions de mise à jour. C'est indispensable pour
écrire, et c'est du travail pur perdu quand on ne fait que lire.

## Explication

**`IQueryable` construit une requête, `IEnumerable` parcourt des objets.** Tant que la chaîne reste en
`IQueryable`, chaque opérateur enrichit l'arbre d'expression qui sera traduit en SQL. Dès qu'elle
bascule en `IEnumerable` — par `AsEnumerable`, `ToList`, ou un `foreach` — la traduction s'arrête et
tout ce qui suit s'exécute en mémoire.

C'est le point de bascule le plus coûteux du framework. Filtrer après `ToList()` signifie rapatrier
toute la table puis jeter 99 % des lignes. Le symptôme est une requête lente sans erreur, et la cause
est invisible dans le code C# : rien ne distingue visuellement `Where` avant et après.

Deux réflexes protègent. Placer systématiquement les filtres, tris et pagination **avant** toute
matérialisation. Et, en cas de doute, journaliser le SQL généré : c'est la seule vérité.

**Le suivi de modifications se désactive en lecture.** Par défaut, chaque entité chargée est suivie :
le contexte conserve une copie de ses valeurs d'origine. Sur une lecture de mille lignes destinée à un
affichage, c'est mille copies inutiles. `AsNoTracking()` supprime ce coût.

La règle est simple : suivi pour écrire, pas de suivi pour lire. Une projection vers un type dédié
n'est de toute façon jamais suivie, puisque le résultat n'est pas une entité.

**Le N+1 est le défaut de performance le plus fréquent.** Charger une liste de commandes puis accéder,
pour chacune, à son client déclenche une requête par commande. Cent commandes produisent cent une
requêtes. En développement sur dix lignes, personne ne le remarque.

Trois réponses, à choisir selon le besoin. `Include` charge la relation en une seule requête et
retourne des entités complètes — pratique quand on va modifier. La **projection** vers un type dédié
ne rapatrie que les colonnes utiles et produit une seule requête — c'est presque toujours le meilleur
choix en lecture. Le chargement explicite convient quand la relation n'est nécessaire que dans
certains cas.

Attention : plusieurs `Include` sur des collections différentes produisent un produit cartésien de
lignes. EF Core propose de scinder la requête pour l'éviter, mais on perd alors l'atomicité de la
lecture. Là encore, la projection est souvent la réponse la plus simple.

**Le chargement paresseux amplifie le problème au lieu de le résoudre.** Quand il est activé, accéder
à une propriété de navigation déclenche silencieusement une requête. Le N+1 devient invisible dans le
code — il suffit d'une boucle d'affichage pour le provoquer. Désactivé, l'accès à une relation non
chargée retourne une collection vide, ce qui est franc et se corrige immédiatement.

**Le contexte est une unité de travail courte.** Il n'est pas conçu pour être partagé entre threads ni
maintenu longtemps : son suivi grossit à mesure qu'on charge, et sa consommation mémoire avec.
Dans une application web, la durée de vie par requête est la bonne. Utiliser une fabrique de contextes
quand on a besoin d'en créer un explicitement, notamment dans un composant à état.

**La concurrence optimiste se déclare par un jeton.** C'est la mise en œuvre .NET du mécanisme décrit
dans `sql-isolation-001`. Une colonne est marquée comme jeton de concurrence : EF Core l'ajoute alors
au `WHERE` de chaque mise à jour. Si aucune ligne n'est affectée, la valeur a changé depuis la
lecture, et une `DbUpdateConcurrencyException` est levée.

L'important est ce qu'on en fait. Attraper l'exception et réessayer aveuglément réintroduit la mise à
jour perdue. Le traitement correct consiste à recharger les valeurs actuelles, décider selon la règle
métier — l'utilisateur gagne, la base gagne, ou on fusionne — et éventuellement redemander à
l'utilisateur. C'est une décision métier, pas technique.

**Les migrations sont du code versionné.** Chaque changement de modèle produit une migration relue
comme du code : une colonne rendue obligatoire sur une table peuplée échouera si les données
existantes ne sont pas d'abord corrigées. Générer une migration ne dispense pas de lire le SQL
qu'elle produira.

## Exemple commenté

Le N+1, puis les deux corrections :

```csharp
// N+1 : une requête pour la liste, puis une par commande pour accéder au client.
List<Order> orders = await context.Orders.Where(o => o.Status == "Paid").ToListAsync(ct);
foreach (Order order in orders)
{
    Console.WriteLine($"{order.OrderId} — {order.Customer.Name}");   // Une requête par tour.
}

// Correction 1 — Include : une seule requête, entités complètes, adapté si l'on va modifier.
List<Order> withCustomer = await context.Orders
    .Where(o => o.Status == "Paid")
    .Include(o => o.Customer)
    .AsNoTracking()                       // Lecture seule : pas de copies de suivi.
    .ToListAsync(ct);

// Correction 2 — projection : une seule requête, et seules les colonnes utiles traversent le réseau.
// Le résultat n'est pas une entité, donc jamais suivi.
List<OrderRow> rows = await context.Orders
    .Where(o => o.Status == "Paid")                       // Traduit en SQL.
    .OrderByDescending(o => o.Total).ThenBy(o => o.OrderId)
    .Select(o => new OrderRow(o.OrderId, o.Customer.Name, o.Total))
    .Take(50)                                             // Pagination AVANT matérialisation.
    .ToListAsync(ct);
```

La gestion d'un conflit de concurrence, avec une décision métier explicite :

```csharp
// La propriété est déclarée jeton de concurrence :
//     builder.Property(o => o.DataVersion).IsConcurrencyToken();
// EF Core ajoute alors « AND DataVersion = @valeurLue » au WHERE de la mise à jour.
try
{
    order.Status = "Paid";
    await context.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException exception)
{
    // Recharger les valeurs réellement en base plutôt que réessayer à l'aveugle.
    PropertyValues? current = await exception.Entries[0].GetDatabaseValuesAsync(ct);
    if (current is null)
    {
        throw new InvalidOperationException("La commande a été supprimée entre-temps.");
    }

    // Décision métier : une commande déjà annulée ne peut pas être marquée payée.
    if (current.GetValue<string>(nameof(Order.Status)) == "Cancelled")
    {
        throw new InvalidOperationException("La commande a été annulée entre-temps.");
    }

    exception.Entries[0].OriginalValues.SetValues(current);
    await context.SaveChangesAsync(ct);
}
```

## Contre-exemple et erreur fréquente

```csharp
public async Task<List<OrderRow>> SearchAsync(string city, int page)
{
    // ToListAsync ici : TOUTE la table est rapatriée, suivie, puis filtrée en mémoire.
    List<Order> all = await _context.Orders.ToListAsync();

    return all
        .Where(o => o.Customer.City == city)     // Filtre en mémoire, et N+1 sur Customer.
        .OrderByDescending(o => o.Total)          // Ordre non total : pagination instable.
        .Skip(page * 20)
        .Take(20)
        .Select(o => new OrderRow(o.OrderId, o.Customer.Name, o.Total))
        .ToList();
}
```

Quatre défauts qui se cumulent, et qui expliquent la quasi-totalité des lenteurs attribuées à
« l'ORM ».

`ToListAsync()` en première ligne matérialise la table entière. Tout ce qui suit s'exécute en
mémoire : le filtre par ville, le tri et la pagination ne parviennent jamais au moteur SQL. Sur un
million de commandes, la méthode rapatrie un million de lignes pour en retourner vingt.

Les entités sont suivies alors qu'elles ne seront jamais modifiées : un million de copies de valeurs
d'origine en mémoire.

`o.Customer.City` sur des entités déjà matérialisées déclenche un chargement par commande — le N+1,
sur un million de lignes cette fois.

Enfin, `OrderByDescending(o => o.Total)` sans critère de départage rend la pagination instable, comme
vu dans `sql-pagination-001`.

La correction consiste à ne rien matérialiser avant d'avoir exprimé filtre, tri et pagination, et à
projeter plutôt que charger des entités :

```csharp
return await _context.Orders
    .Where(o => o.Customer.City == city)                  // Traduit en JOIN + WHERE.
    .OrderByDescending(o => o.Total).ThenBy(o => o.OrderId)
    .Skip(page * 20).Take(20)
    .Select(o => new OrderRow(o.OrderId, o.Customer.Name, o.Total))
    .ToListAsync(ct);                                     // Une seule requête, vingt lignes.
```

## Vérification de compréhension

Dites à quel moment précis la requête part en base dans les deux versions ci-dessus, et combien de
lignes traversent le réseau dans chacune.

:::quiz
id=ef-core-data-access-001-check
question=Une méthode appelle ToListAsync() puis enchaîne Where, OrderBy et Take. Que se passe-t-il ?
option=EF Core réordonne les opérateurs et produit une requête SQL optimale
option=La table entière est rapatriée puis filtrée en mémoire : filtre, tri et pagination ne parviennent jamais au moteur
option=Une exception est levée, car ces opérateurs ne sont pas valides après matérialisation
correct=1
success=Correct : la matérialisation arrête la traduction. Tout ce qui suit s'exécute en mémoire sur des données déjà rapatriées.
retry=Relisez le passage sur le point de bascule entre requête traduite et objets en mémoire, et ce qui le déclenche.
:::

## Exercice guidé

Ouvrez le scénario `sql-concurrency-candidates-001` dans `/sql-lab`, puis procédez ainsi.

1. Identifiez la colonne qui sert de jeton de concurrence dans le schéma du laboratoire.
2. Écrivez la mise à jour conditionnée par la version lue, et prédisez le nombre de lignes affectées.
3. Rejouez avec une version périmée et observez le résultat à zéro : c'est exactement ce qu'EF Core
   traduit en `DbUpdateConcurrencyException`.
4. Validez contre la référence, puis réinitialisez la session.

Les scénarios `ef-orders-tracking-001`, `ef-orders-loading-001`, `ef-orders-queryable-001` et
`ef-orders-concurrency-001` sous `content/sql/` portent le code C# correspondant, exécuté dans le
runner isolé : lisez leur starter et leur solution après avoir fait l'exercice SQL.

## Exercice autonome

Écrivez la méthode de recherche paginée des commandes d'une ville, avec le nom du client et le montant.

Décidez avant de coder : le type de retour, l'emplacement de la matérialisation, le choix entre
inclusion et projection, l'usage ou non du suivi, l'ordre total, et la borne de taille de page.
Justifiez chaque décision en une phrase, puis vérifiez le SQL généré.

## Débogage

Un ticket indique : « L'écran de liste met huit secondes à s'afficher, alors que la table ne contient
que quelques milliers de lignes. »

1. **Symptôme** : lenteur sans rapport avec le volume, aucune erreur.
2. **Hypothèse** : un N+1, ou une matérialisation prématurée qui déplace le filtre en mémoire.
3. **Preuve** : activez la journalisation des commandes générées et comptez les requêtes émises pour
   un seul affichage. Un nombre proportionnel au nombre de lignes affichées confirme le N+1.
4. **Prévention** : projeter vers un type dédié, déplacer la matérialisation en fin de chaîne, et
   ajouter un test qui compte les commandes émises pour un appel.

## Entretien

Question posée à voix haute : *comment détectez-vous et corrigez-vous un N+1 ?*

Une réponse solide décrit le symptôme — nombre de requêtes proportionnel au nombre de lignes — cite la
journalisation du SQL comme moyen de preuve, et distingue inclusion et projection selon qu'on va
modifier ou seulement lire. Elle mentionne aussi le chargement paresseux comme facteur aggravant.

## Résumé

- Tant que la chaîne reste une requête, elle est traduite ; matérialiser arrête la traduction.
- Filtre, tri et pagination s'expriment avant toute matérialisation.
- Suivi pour écrire, `AsNoTracking` ou projection pour lire.
- Le N+1 se corrige par inclusion ou par projection, selon l'intention.
- Un conflit de concurrence se résout par une décision métier, jamais par un réessai aveugle.

## Cartes de révision

Question : pourquoi une projection vers un type dédié n'est-elle jamais suivie ? Réponse attendue : le
résultat n'est pas une entité du modèle, donc le contexte n'a rien à comparer.

Question : que fait EF Core d'une propriété déclarée jeton de concurrence ? Réponse attendue : il
l'ajoute au `WHERE` de la mise à jour et lève si aucune ligne n'est affectée.

## Test de maîtrise

Sans relire, écrivez la méthode qui retourne les vingt dernières commandes d'un client avec le nom du
produit de chaque ligne, sans N+1 et sans suivi. Justifiez l'emplacement de la matérialisation, le
choix entre inclusion et projection, et décrivez le test qui prouverait qu'une seule requête est
émise.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
