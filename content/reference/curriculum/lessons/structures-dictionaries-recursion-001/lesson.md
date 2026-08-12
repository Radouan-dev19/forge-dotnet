# Dictionnaires et récursivité bornée

## Objectif observable

À la fin de cette leçon, vous saurez remplacer une recherche imbriquée par un accès indexé et
justifier le coût mémoire consenti, et vous saurez écrire une récursion dont vous pouvez prouver la
terminaison et estimer la profondeur maximale.

## Prérequis

- Avoir lu `structures-stacks-queues-001` et savoir choisir une structure par intention.
- Savoir déclarer une classe et surcharger une méthode.

## Intuition

Un dictionnaire répond à une question précise : *quelle valeur est associée à cette clé ?* Il y
répond en temps constant en moyenne, quel que soit le nombre d'entrées. Le prix est la mémoire et
l'absence d'ordre garanti.

Une récursion, elle, résout un problème en s'appelant sur un problème **strictement plus petit**.
Les deux mots comptent : si la réduction n'est pas stricte, la récursion ne termine pas ; si elle
n'est pas bornée, elle sature la pile d'appels.

## Explication

**Le dictionnaire supprime la boucle imbriquée.** Le motif à reconnaître : une boucle qui, pour chaque
élément, en cherche un autre dans une seconde collection. C'est O(n × m). Indexer la seconde
collection par sa clé une seule fois, puis interroger l'index dans la boucle, donne O(n + m). Sur
deux collections de mille éléments, on passe d'un million d'opérations à deux mille.

C'est probablement l'optimisation la plus rentable et la plus fréquente de tout le métier. Elle ne
demande aucune astuce : seulement de reconnaître le motif.

**Le coût moyen n'est pas le coût garanti.** L'accès par clé est en O(1) **en moyenne**. Le pire cas
théorique est O(n), lorsque toutes les clés tombent dans le même seau. En pratique cela n'arrive que
si la fonction de hachage est mauvaise — ou si un attaquant choisit les clés délibérément, ce qui est
une classe d'attaque réelle sur les paramètres de requête. Pour une clé maîtrisée par le code, la
moyenne est le bon modèle mental.

**`GetHashCode` et `Equals` forment un contrat indissociable.** Deux objets égaux **doivent** produire
le même code de hachage ; l'inverse n'est pas exigé. Si vous surchargez `Equals` sans surcharger
`GetHashCode`, deux objets égaux atterrissent dans des seaux différents et le dictionnaire ne
retrouve jamais l'entrée : la valeur est présente, la recherche échoue. C'est un bug qui survit
longtemps parce qu'il ne lève aucune exception.

Corollaire souvent ignoré : le code de hachage ne doit dépendre que de champs **immuables**. Modifier
un champ qui participe au hachage après insertion rend l'entrée inaccessible — elle occupe la mémoire
et personne ne peut plus la lire. En C#, un `record` avec des propriétés en lecture seule règle les
deux problèmes d'un coup, puisque l'égalité structurelle et le hachage sont générés ensemble.

**Choisir le bon comparateur.** Pour une clé textuelle, `StringComparer.Ordinal` est déterministe et
rapide ; `StringComparer.OrdinalIgnoreCase` ajoute l'insensibilité à la casse sans dépendre de la
culture. Ne jamais s'en remettre au comparateur par défaut pour une clé technique — c'est le même
raisonnement que dans `strings-dates-001`.

**Interroger sans lever.** `dictionary[key]` lève si la clé est absente : c'est le bon choix quand
l'absence est une violation de contrat. `TryGetValue` répond par un booléen : c'est le bon choix
quand l'absence est attendue. `GetValueOrDefault` fournit un repli. Le choix exprime le contrat, comme
vu dans `csharp-exceptions-nullable-001`.

**Une récursion utile tient en trois lignes de raisonnement.** *Cas de base* : quelle entrée se résout
sans appel récursif ? *Réduction* : en quoi l'appel suivant porte-t-il sur un problème strictement
plus petit ? *Profondeur* : combien d'appels imbriqués au maximum ?

La troisième question est celle qu'on saute, et c'est celle qui casse en production. La pile d'appels
.NET vaut un mégaoctet par défaut, soit quelques dizaines de milliers de niveaux. Une récursion sur
une liste chaînée de cent mille éléments lève `StackOverflowException` — une exception qu'on **ne peut
pas attraper** et qui termine le processus immédiatement.

La règle pratique : une récursion dont la profondeur dépend de la **taille des données** doit être
dérécursivée avec une pile explicite. Une récursion dont la profondeur dépend de la **structure** —
la hauteur d'un arbre équilibré, par exemple — est généralement sûre.

**La mémoïsation relie les deux sujets.** Une récursion qui recalcule les mêmes sous-problèmes peut
devenir exponentielle. Mémoriser les résultats déjà calculés dans un dictionnaire ramène le coût au
nombre de sous-problèmes distincts. C'est le pont naturel entre les deux moitiés de cette leçon.

## Exemple commenté

Le motif d'indexation, avant et après :

```csharp
// Avant : pour chaque commande, on parcourt tous les clients. O(commandes × clients).
public static List<string> LabelsSlow(IReadOnlyList<Order> orders, IReadOnlyList<Customer> customers)
{
    var labels = new List<string>();
    foreach (Order order in orders)
    {
        Customer? match = customers.FirstOrDefault(c => c.Id == order.CustomerId);
        labels.Add($"{order.Reference} — {match?.Name ?? "inconnu"}");
    }

    return labels;
}

// Après : on indexe une fois, puis chaque recherche est en O(1) moyen. O(commandes + clients).
public static List<string> Labels(IReadOnlyList<Order> orders, IReadOnlyList<Customer> customers)
{
    var byId = customers.ToDictionary(customer => customer.Id);   // Lève si un Id est en doublon.
    var labels = new List<string>();
    foreach (Order order in orders)
    {
        // L'absence est attendue : TryGetValue plutôt que l'indexeur.
        string name = byId.TryGetValue(order.CustomerId, out Customer? match) ? match.Name : "inconnu";
        labels.Add($"{order.Reference} — {name}");
    }

    return labels;
}
```

Une récursion dont on peut prouver les trois propriétés :

```csharp
// Cas de base   : un nœud sans enfant a pour hauteur 1.
// Réduction     : chaque appel porte sur un sous-arbre strictement plus petit.
// Profondeur max: la hauteur de l'arbre — sûre si l'arbre est équilibré, à surveiller sinon.
public static int Height(TreeNode? node) =>
    node is null ? 0 : 1 + Math.Max(Height(node.Left), Height(node.Right));
```

## Contre-exemple et erreur fréquente

```csharp
public sealed class ProductKey
{
    public string Code { get; set; } = "";

    public override bool Equals(object? other) =>
        other is ProductKey key && key.Code == Code;
    // GetHashCode n'est pas surchargé : il reste celui de la référence.
}

// Conséquence :
var stock = new Dictionary<ProductKey, int>();
stock[new ProductKey { Code = "A1" }] = 10;
bool found = stock.ContainsKey(new ProductKey { Code = "A1" });   // false !
```

Les deux clés sont égales selon `Equals`, mais leurs codes de hachage diffèrent — celui par défaut
dépend de la référence. Le dictionnaire cherche donc dans le mauvais seau et ne trouve rien. La valeur
est bien présente en mémoire ; elle est simplement devenue inaccessible.

Aucune exception n'est levée. Le symptôme est un stock qui affiche zéro, ou un cache qui n'obtient
jamais de succès et recalcule tout — un bug de performance qu'on met des semaines à relier à sa
cause.

Le défaut est aggravé par le setter public sur `Code` : même avec `GetHashCode` correctement
surchargé, modifier `Code` après insertion déplacerait la clé dans un autre seau et rendrait l'entrée
introuvable. La forme correcte tient en une ligne, et règle les deux problèmes :

```csharp
public sealed record ProductKey(string Code);
```

## Vérification de compréhension

Énoncez le contrat qui lie `Equals` et `GetHashCode`, puis expliquez pourquoi une clé mutable est
dangereuse même quand les deux sont correctement surchargés.

:::quiz
id=structures-dictionaries-recursion-001-check
question=Une classe surcharge Equals mais pas GetHashCode, et sert de clé de dictionnaire. Que se passe-t-il ?
option=Une exception est levée à l'insertion, car le contrat n'est pas respecté
option=Deux clés égales tombent dans des seaux différents : la valeur est stockée mais devient introuvable, sans erreur
option=Le dictionnaire retombe automatiquement sur Equals et fonctionne correctement, en O(n)
correct=1
success=Correct : la recherche commence par le code de hachage. S'il diffère pour deux objets égaux, le bon seau n'est jamais consulté, et le défaut reste silencieux.
retry=Relisez le contrat entre Equals et GetHashCode, et suivez ce que fait le dictionnaire pour localiser une entrée.
:::

## Exercice guidé

Ouvrez `structures-frequency-map-001` dans `/practice`, puis procédez ainsi.

1. Écrivez le type exact de la clé et le comparateur retenu avant tout code.
2. Implémentez le comptage en un seul parcours, avec `GetValueOrDefault`.
3. Annoncez la complexité en temps et en espace, et justifiez l'échange consenti.
4. Vérifiez le comportement sur collection vide et sur une clé présente une seule fois.

## Exercice autonome

Écrivez une méthode qui, à partir d'une liste de commandes et d'une liste de clients, retourne le
chiffre d'affaires par ville.

Décidez avant de coder : la clé du dictionnaire et son comparateur, le traitement d'une commande dont
le client est introuvable, celui d'une ville absente, et la complexité visée. Justifiez le choix entre
l'indexeur, `TryGetValue` et `GetValueOrDefault`.

## Débogage

Un ticket indique : « Le cache ne sert jamais : chaque appel recalcule tout. »

1. **Symptôme** : le taux de succès du cache est nul, sans erreur ni exception.
2. **Hypothèse** : la clé du cache ne respecte pas le contrat d'égalité et de hachage.
3. **Preuve** : construisez deux clés que vous jugez identiques, puis comparez `Equals` et
   `GetHashCode` sur les deux. Une égalité vraie avec des hachages différents confirme l'hypothèse.
4. **Prévention** : transformez la clé en `record` à propriétés en lecture seule, et ajoutez un test
   qui vérifie qu'un aller-retour d'insertion puis de lecture réussit.

## Entretien

Question posée à voix haute : *quand transformez-vous une récursion en boucle ?*

Une réponse solide part de la profondeur maximale et de son lien avec la taille des données, cite
`StackOverflowException` et le fait qu'elle ne peut pas être attrapée, et sait dire que la
transformation consiste à porter l'état sur une pile explicite. Elle reconnaît aussi que la récursion
reste préférable quand la profondeur est bornée par la structure.

## Résumé

- Indexer une collection par clé transforme un O(n × m) en O(n + m).
- L'accès par clé est O(1) en moyenne, O(n) au pire ; le hachage décide.
- `Equals` et `GetHashCode` se surchargent ensemble, sur des champs immuables.
- Une récursion exige cas de base, réduction stricte et profondeur estimée.
- Une profondeur qui dépend de la taille des données doit être dérécursivée.

## Cartes de révision

Question : pourquoi une clé de dictionnaire doit-elle être immuable ? Réponse attendue : modifier un
champ participant au hachage déplace la clé de seau et rend l'entrée définitivement introuvable.

Question : quelle question saute-t-on le plus souvent en écrivant une récursion ? Réponse attendue :
la profondeur maximale des appels imbriqués.

## Test de maîtrise

Sans relire, écrivez une méthode récursive qui calcule le nombre de nœuds d'un arbre, puis sa version
itérative avec une pile explicite. Justifiez la profondeur maximale de la version récursive, indiquez à
partir de quelle taille de données vous basculeriez, et écrivez le test qui prouve que les deux
versions retournent la même valeur.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
