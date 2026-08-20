# LINQ : requêtes lisibles et évaluations maîtrisées

## Objectif observable

À la fin de cette leçon, vous saurez dire, pour une chaîne LINQ donnée, **quand** elle s'exécute et
**combien de fois** la source est parcourue, et vous saurez placer le `ToList()` au seul endroit où
il est nécessaire.

## Prérequis

- Avoir lu `generics-delegates-001` et savoir qu'une lambda doit rester pure.
- Savoir écrire un `foreach` et une expression lambda.

## Intuition

Une chaîne LINQ ne calcule rien tant que personne ne demande un résultat. `Where` et `Select` ne font
que **décrire** une intention ; c'est le `foreach`, le `ToList()` ou le `Count()` qui déclenche
réellement le parcours.

Cette propriété s'appelle l'exécution différée. Elle est très utile — on peut composer une requête en
plusieurs étapes sans rien payer — et elle est la source des deux surprises les plus fréquentes : le
travail refait plusieurs fois, et le résultat qui change entre deux lectures.

## Explication

**Différé contre immédiat.** `Where`, `Select`, `OrderBy`, `Take`, `Skip` sont différés : ils
retournent une description. `ToList`, `ToArray`, `Count`, `Sum`, `First`, `Any` sont immédiats : ils
parcourent et produisent une valeur. La règle mnémotechnique : si l'opérateur retourne encore un
`IEnumerable<T>`, rien n'a été exécuté.

`OrderBy` occupe une place à part : il est différé, mais lorsqu'il s'exécute, il doit **matérialiser
toute la source** pour trier. Un `OrderBy` suivi d'un `First()` charge donc l'ensemble en mémoire, là
où une simple recherche du minimum n'en aurait pas eu besoin.

**Le piège de la double énumération.** Une variable qui contient une chaîne différée n'est pas un
résultat : c'est une recette. L'utiliser deux fois exécute la recette deux fois.

```csharp
IEnumerable<Order> recent = orders.Where(o => o.IsRecent);
int count = recent.Count();          // Premier parcours complet.
decimal total = recent.Sum(o => o.Total);   // Second parcours complet.
```

Sur une liste en mémoire, le coût est un facteur deux. Sur une source coûteuse — une lecture de
fichier, une requête base de données via `IQueryable`, un flux réseau — c'est deux appels réels. Le
remède est un `ToList()` unique, placé au moment où la requête est terminée et où le résultat va
servir plusieurs fois.

**Le piège du résultat qui change.** Comme la requête est réévaluée à chaque parcours, elle reflète
l'état de la source **au moment du parcours**, pas au moment de la déclaration. Si la source est
modifiée entre-temps, la deuxième lecture donne autre chose. Ce comportement est parfois voulu ; s'il
ne l'est pas, matérialisez.

**Où placer `ToList()`.** Trop tôt, il tue la composition : `orders.ToList().Where(...)` charge tout
puis filtre. Trop tard ou jamais, il laisse la double énumération. La règle pratique : matérialisez
**une fois**, à la frontière — quand la requête sort de la méthode qui l'a construite, ou quand le
résultat va être lu plusieurs fois.

**`IEnumerable<T>` et `IQueryable<T>` ne sont pas interchangeables.** Sur `IQueryable<T>`, la lambda
est traduite en SQL par le fournisseur ; sur `IEnumerable<T>`, elle est exécutée en mémoire.
Transtyper l'un en l'autre trop tôt — ou appeler `AsEnumerable()` par réflexe — rapatrie toute la
table avant de filtrer. Le symptôme est une requête lente sans erreur apparente. Ce point sera repris
en profondeur avec EF Core.

**Choisir l'opérateur qui dit la vérité.** `Any()` s'arrête au premier élément trouvé ;
`Count() > 0` parcourt tout. `First()` lève si la source est vide, `FirstOrDefault()` retourne la
valeur par défaut : le choix doit refléter le contrat — absence attendue ou violation, comme vu dans
`csharp-exceptions-nullable-001`. `Single()` affirme en plus l'unicité et lève s'il y a deux
éléments : c'est une assertion utile quand l'unicité est un invariant.

## Exemple commenté

L'exécution différée, rendue visible par un compteur :

```csharp
int inspected = 0;
int[] amounts = [12, -3, 40, 7];

IEnumerable<int> positives = amounts.Where(amount =>
{
    inspected++;              // Effet de bord, ici uniquement pour observer.
    return amount > 0;
});

Console.WriteLine(inspected); // 0 : rien n'a encore été parcouru.

int[] materialized = positives.ToArray();
Console.WriteLine(inspected); // 4 : la source a été parcourue une fois.

Console.WriteLine(positives.Count());
Console.WriteLine(inspected); // 8 : la recette a été rejouée intégralement.
```

La troisième valeur est celle qui surprend. `positives` n'a jamais contenu de résultat : chaque usage
relance le filtrage. La version correcte matérialise une fois et lit ensuite le tableau :

```csharp
int[] positives = amounts.Where(amount => amount > 0).ToArray();
Console.WriteLine(positives.Length);
Console.WriteLine(positives.Sum());   // Aucun nouveau parcours de la source.
```

## Contre-exemple et erreur fréquente

```csharp
public static string Summarize(IEnumerable<Order> orders)
{
    var paid = orders.Where(o => o.Status == "Paid").OrderByDescending(o => o.Total);

    // Trois usages, trois parcours complets de la source — et un tri refait trois fois.
    return $"{paid.Count()} commandes, top {paid.First().Reference}, total {paid.Sum(o => o.Total)}";
}
```

La méthode reçoit un `IEnumerable<Order>` dont elle ignore l'origine. Si l'appelant lui passe une
requête EF Core, ce sont **trois** allers-retours vers la base, chacun avec son tri. Si la source est
un flux non rejouable, le deuxième parcours retourne une séquence vide et `First()` lève.

Le `OrderByDescending` aggrave le coût : il matérialise et trie l'intégralité de la source à chaque
parcours, alors que le seul élément utile pour `First()` est le maximum.

La correction tient en une ligne, et elle rend le coût explicite :

```csharp
public static string Summarize(IEnumerable<Order> orders)
{
    Order[] paid = orders.Where(o => o.Status == "Paid")
                         .OrderByDescending(o => o.Total)
                         .ToArray();   // Un seul parcours, un seul tri.

    return paid.Length == 0
        ? "Aucune commande payée"
        : $"{paid.Length} commandes, top {paid[0].Reference}, total {paid.Sum(o => o.Total)}";
}
```

Le cas de la collection vide, invisible dans la première version, devient traité : `First()` aurait
levé.

## Vérification de compréhension

Pour une chaîne `source.Where(...).Select(...)` stockée dans une variable puis lue deux fois, dites
combien de fois la source est parcourue et pourquoi.

:::quiz
id=linq-lambdas-001-check
question=Une variable contient `orders.Where(o => o.IsRecent)`. Que se passe-t-il si on appelle Count() puis Sum() dessus ?
option=Le résultat est calculé une fois puis mis en cache par LINQ
option=La source est parcourue deux fois, car la variable contient une requête différée et non un résultat
option=Le second appel lève une exception, la séquence ayant déjà été consommée
correct=1
success=Correct : un opérateur différé retourne une description ; chaque opérateur immédiat relance le parcours complet de la source.
retry=Relisez l'exemple du compteur : la troisième valeur affichée montre que la recette est rejouée intégralement.
:::

## Exercice guidé

Ouvrez `csharp-linq-top-three-001` dans `/practice`, puis procédez ainsi.

1. Écrivez la chaîne LINQ, puis annotez chaque opérateur en « différé » ou « immédiat ».
2. Indiquez le nombre de parcours de la source avant d'exécuter.
3. Placez un seul `ToArray()` et justifiez sa position.
4. Vérifiez votre prédiction en instrumentant temporairement le prédicat avec un compteur.

## Exercice autonome

Écrivez une méthode qui reçoit des mesures et retourne, en une seule matérialisation : le nombre de
valeurs retenues, leur moyenne, et les trois plus élevées.

Décidez avant de coder : où placer la matérialisation, le comportement sur source vide, et le type de
retour. Justifiez le choix entre `Any()` et `Count() > 0` pour tester la vacuité.

## Débogage

Un ticket indique : « La page de statistiques met huit secondes à s'afficher, alors que la table ne
contient que 400 lignes. »

1. **Symptôme** : la lenteur est sans rapport avec le volume de données.
2. **Hypothèse** : une requête différée est énumérée plusieurs fois, une fois par indicateur affiché.
3. **Preuve** : activez la journalisation des commandes du fournisseur et comptez les requêtes émises
   pour un seul affichage.
4. **Prévention** : matérialisez une fois avant de calculer les indicateurs, et ajoutez un test qui
   compte les énumérations d'une source instrumentée.

## Entretien

Question posée à voix haute : *qu'est-ce que l'exécution différée en LINQ, et quel problème avez-vous
déjà rencontré à cause d'elle ?*

Une réponse solide distingue description et exécution, cite un cas vécu de double énumération ou de
résultat modifié entre deux lectures, et explique où le `ToList()` a été placé et pourquoi à cet
endroit précis.

## Résumé

- Un opérateur qui retourne `IEnumerable<T>` n'a rien exécuté.
- Une variable contenant une requête est une recette, pas un résultat.
- Matérialiser une fois, à la frontière, quand le résultat sert plusieurs fois.
- `OrderBy` charge toute la source pour trier, même suivi d'un `First()`.
- `Any()` s'arrête au premier élément ; `Count() > 0` parcourt tout.

## Cartes de révision

Question : comment reconnaître un opérateur LINQ différé ? Réponse attendue : il retourne encore une
séquence, donc rien n'est parcouru tant qu'un opérateur immédiat ne le demande pas.

Question : quel risque prend une méthode qui reçoit `IEnumerable<T>` et l'énumère trois fois ? Réponse
attendue : trois parcours réels de la source, voire une séquence vide si elle n'est pas rejouable.

## Test de maîtrise

Sans relire, écrivez une méthode qui produit un rapport contenant un décompte, une somme et le
meilleur élément, à partir d'une source `IEnumerable<T>` d'origine inconnue. Justifiez l'emplacement
unique de la matérialisation, traitez la source vide, et expliquez comment vous prouveriez par un test
que la source n'est parcourue qu'une seule fois.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
