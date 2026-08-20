# Tableaux et listes : choisir la bonne forme

## Objectif observable

À la fin de cette leçon, vous saurez justifier le choix entre `int[]`, `List<T>` et
`IReadOnlyList<T>` sur un cas donné, et vous saurez écrire une transformation qui produit une
nouvelle collection sans jamais modifier celle qu'elle a reçue.

## Prérequis

- Avoir lu `csharp-io-debugger-001` et savoir convertir une entrée externe sans planter.
- Savoir parcourir une collection avec `foreach`.

## Intuition

Un tableau annonce une taille fixée à la création. Une liste annonce une collection qui grandit et
rétrécit. Le choix n'est donc pas une question de confort d'écriture : il **documente une
intention**, et cette intention est lue par le prochain développeur.

La seconde décision, souvent plus lourde de conséquences, est de savoir si votre méthode a le droit
de modifier la collection qu'on lui confie.

## Explication

**Taille fixe contre taille variable.** `int[] scores = new int[5]` réserve cinq cases, et la taille
ne changera plus. `List<int>` gère un tableau interne qu'elle remplace par un plus grand lorsqu'il
est plein. Ce redimensionnement a un coût amorti négligeable, mais il n'est pas gratuit : si vous
connaissez la taille finale, `new List<int>(capacity: 1000)` évite plusieurs recopies.

**Le type de retour est un contrat.** Retourner `List<T>` autorise l'appelant à ajouter et supprimer
des éléments de votre collection interne. C'est rarement voulu. `IReadOnlyList<T>` annonce
l'inverse : on peut lire et indexer, pas modifier. `IEnumerable<T>` annonce encore moins — on peut
parcourir, une fois, sans connaître la taille. Choisissez le type le plus faible qui suffise à
l'appelant : il pourra être renforcé plus tard sans rien casser.

Attention à une nuance : `IReadOnlyList<T>` empêche la modification **par cette référence**. Si vous
retournez directement votre `List<T>` sous ce type, un appelant qui la transtype retrouve l'accès en
écriture. Pour une garantie réelle, retournez une copie ou un `ReadOnlyCollection<T>`.

**Modifier ou produire.** Deux styles coexistent en C#. Le style *mutant* modifie la collection reçue
et retourne `void` ; le style *producteur* laisse l'entrée intacte et retourne une nouvelle
collection. Les deux sont légitimes, mais ils doivent être annoncés par le nom :
`SortInPlace(scores)` contre `decimal[] Normalized(decimal[] amounts)`. Le piège est la méthode qui
s'appelle `Normalize` et qui, silencieusement, écrase l'entrée de l'appelant.

**Ne modifiez jamais pendant une énumération.** Ajouter ou retirer un élément d'une `List<T>` pendant
un `foreach` lève une `InvalidOperationException`. Ce n'est pas une limitation arbitraire : l'énumérateur
a mémorisé une version de la collection, et la modification l'a invalidé. Le remède mécanique consiste
à parcourir une copie (`foreach (var item in list.ToList())`) ou à collecter les éléments à retirer
puis à les retirer après la boucle. Mais l'exception est souvent le symptôme d'un problème plus
profond : la boucle fait deux choses à la fois, sélectionner et modifier.

**Les tableaux sont covariants, et c'est un piège.** `object[] o = new string[2];` compile, puis
`o[0] = 42;` lève une `ArrayTypeMismatchException` à l'exécution. `List<T>` n'a pas ce comportement.
C'est une raison de plus de préférer les collections génériques dès qu'il y a du polymorphisme.

## Exemple commenté

Une transformation qui produit au lieu de modifier :

```csharp
public static int[] PositiveDifferences(int[] values)
{
    ArgumentNullException.ThrowIfNull(values);

    // La taille du résultat n'est pas connue à l'avance : une liste convient pour construire…
    var differences = new List<int>(Math.Max(0, values.Length - 1));
    for (int index = 1; index < values.Length; index++)
    {
        int delta = values[index] - values[index - 1];
        if (delta > 0)
        {
            differences.Add(delta);
        }
    }

    // …et un tableau fige le résultat, dont la taille ne bougera plus.
    return differences.ToArray();
}
```

`values` est lu, jamais écrit : l'appelant peut réutiliser son tableau après l'appel. La liste sert
d'échafaudage interne et n'est pas exposée. Le nom au pluriel annonce une collection en retour.

## Contre-exemple et erreur fréquente

```csharp
public static void Normalize(List<decimal> amounts)
{
    foreach (decimal amount in amounts)
    {
        if (amount < 0)
        {
            amounts.Remove(amount);   // InvalidOperationException au tour suivant.
        }
    }
}
```

Deux erreurs se superposent. La modification pendant l'énumération lève une exception dès le premier
retrait. Et même corrigée, la méthode reste un piège : elle s'appelle `Normalize`, ne retourne rien,
et détruit la collection de l'appelant. Un code qui affiche le total avant et après l'appel obtient
deux valeurs différentes sans qu'aucune ligne ne le laisse deviner.

La correction complète change la signature :

```csharp
public static IReadOnlyList<decimal> WithoutNegatives(IReadOnlyList<decimal> amounts)
{
    ArgumentNullException.ThrowIfNull(amounts);
    var kept = new List<decimal>(amounts.Count);
    foreach (decimal amount in amounts)
    {
        if (amount >= 0)
        {
            kept.Add(amount);
        }
    }

    return kept;
}
```

Le nom annonce le résultat, le paramètre annonce la lecture seule, et l'entrée reste intacte.

## Vérification de compréhension

Pour une méthode qui reçoit des mesures et retourne les écarts positifs, nommez : le type du
paramètre, le type de retour, et ce que l'appelant peut supposer de sa collection après l'appel.

:::quiz
id=collections-arrays-001-check
question=Une méthode publique construit une collection en interne puis la retourne. Quel type de retour annonce le mieux qu'elle ne doit pas être modifiée ?
option=Le type concret List, car il est le plus courant et le plus pratique
option=Une liste en lecture seule, qui autorise la lecture et l'indexation sans exposer les mutations
option=object[], qui accepte tous les types d'éléments
correct=1
success=Correct : le type le plus faible qui suffise à l'appelant documente l'intention et pourra être renforcé plus tard sans rompre le contrat.
retry=Relisez le passage sur le type de retour comme contrat, et notez la nuance sur la garantie réelle en cas de transtypage.
:::

## Exercice guidé

Ouvrez `csharp-array-positive-sum-001` dans `/practice`, puis procédez ainsi.

1. Écrivez la signature en choisissant le type de paramètre le plus faible qui suffise.
2. Listez les cas : tableau vide, un seul élément, que des négatifs, mélange.
3. Implémentez sans modifier l'entrée, puis vérifiez ce point par une assertion explicite.
4. Comparez vos prédictions aux résultats et notez tout écart.

## Exercice autonome

Écrivez une méthode qui reçoit des identifiants de commande et retourne les doublons, chacun une
seule fois, dans l'ordre de première apparition.

Décidez avant de coder : le type de paramètre, le type de retour, le comportement sur collection
vide, et si l'ordre de sortie fait partie du contrat. Justifiez la complexité de votre approche en
fonction du nombre d'identifiants.

## Débogage

Un ticket indique : « Après affichage du panier, le total du récapitulatif ne correspond plus. »

1. **Symptôme** : le total change entre deux lectures, sans action de l'utilisateur.
2. **Hypothèse** : une méthode d'affichage modifie la collection qu'elle reçoit.
3. **Preuve** : notez le nombre d'éléments avant et après l'appel suspect, sans modifier les données.
   Une différence prouve la mutation.
4. **Prévention** : changez le paramètre en `IReadOnlyList<T>` — la mutation ne compilera plus — et
   ajoutez un test qui vérifie que l'entrée est inchangée après l'appel.

## Entretien

Question posée à voix haute : *quand retournez-vous `IEnumerable<T>` plutôt qu'une liste concrète ?*

Une réponse solide distingue trois choses : ce dont l'appelant a besoin, ce que le type promet, et le
risque d'énumération multiple lorsque la source est différée. Elle cite un cas où `IEnumerable<T>`
était le bon choix et un cas où il a causé un recalcul coûteux.

## Résumé

- Un tableau annonce une taille fixe, une liste une collection qui évolue.
- Le type de retour est un contrat : choisissez le plus faible qui suffise.
- Un nom doit dire si la méthode produit ou si elle modifie.
- Modifier pendant une énumération invalide l'itérateur, et signale souvent une boucle qui fait deux choses.

## Cartes de révision

Question : que promet `IReadOnlyList<T>` en retour, et que ne promet-il pas ? Réponse attendue : pas
de mutation par cette référence, mais aucune garantie si l'appelant transtype vers la liste concrète.

Question : quel indice, dans un nom de méthode, annonce une mutation de l'entrée ? Réponse attendue :
un verbe à l'impératif sans valeur de retour, du type `SortInPlace` ou `Clear`.

## Test de maîtrise

Sans relire, concevez la signature d'une méthode qui fusionne deux collections de mesures en
supprimant les doublons. Justifiez le type de chaque paramètre, le type de retour, le comportement
sur entrée vide, et écrivez le test qui prouve que les deux entrées sont intactes après l'appel.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
