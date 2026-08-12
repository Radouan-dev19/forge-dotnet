# Génériques et delegates utiles

## Objectif observable

À la fin de cette leçon, vous saurez rendre générique une méthode qui ne dépend pas d'un type
concret, choisir la contrainte minimale qui rend le corps compilable, et injecter un comportement par
`Func<>` sans transformer votre code en énigme.

## Prérequis

- Avoir lu `csharp-exceptions-nullable-001` et savoir distinguer absence et violation de contrat.
- Savoir écrire une méthode statique et une expression lambda simple.

## Intuition

Un générique répond à la question : *mon algorithme change-t-il selon le type des données ?* Compter
les éléments d'une collection ne dépend pas de leur type ; additionner leurs montants, si.

Un delegate répond à une autre question : *quelle partie de ce traitement doit être décidée par
l'appelant ?* Filtrer une collection, c'est toujours parcourir et retenir — seul le critère change.

## Explication

**Le générique préserve le type.** Avant les génériques, on passait par `object` : on perdait le type
à l'entrée et on le rétablissait par un transtypage à la sortie. Les erreurs se déplaçaient de la
compilation vers l'exécution, et le boxing des types valeur coûtait une allocation par élément.
`T Max<T>(T a, T b)` conserve le type de bout en bout : `Max(1, 2)` retourne un `int`, sans
transtypage ni allocation.

**Les contraintes ouvrent exactement ce dont le corps a besoin.** `where T : IComparable<T>` autorise
la comparaison. `where T : class` autorise la comparaison à `null`. `where T : new()` autorise
l'instanciation. La règle est de n'ajouter que la contrainte que le corps exige réellement : chaque
contrainte supplémentaire réduit l'ensemble des types acceptés sans rien apporter.

Un cas fréquent mérite d'être connu : pour comparer sans imposer `IComparable<T>` à l'appelant,
acceptez un `IComparer<T>` en paramètre. Le type reste libre, la stratégie de comparaison devient
explicite.

**Les delegates prédéfinis suffisent presque toujours.** `Func<int, bool>` désigne une fonction d'un
`int` vers un `bool` ; `Action<string>` une opération sans retour ; `Predicate<T>` un test. Déclarer
son propre type `delegate` ne se justifie que lorsque le nom apporte une information que la signature
ne donne pas.

Le paramètre porte alors le sens : `Where(Func<T, bool> predicate)` se lit sans documentation, tandis
que `Apply(Func<T, T> f)` oblige à lire l'implémentation. Nommez le paramètre d'après son rôle métier,
pas d'après sa forme.

**Une lambda doit rester une expression, pas un programme.** Une lambda de trois lignes qui lit un
fichier et met à jour un compteur externe est difficile à lire et à tester. Extrayez-la dans une
méthode nommée et passez le groupe de méthodes : `values.Where(IsEligible)` se lit mieux que la même
condition inline.

**Les effets de bord dans une lambda sont un piège.** Le nombre et l'ordre des appels d'une lambda ne
sont pas toujours ceux qu'on imagine — c'est particulièrement vrai avec les opérateurs différés vus
dans `linq-lambdas-001`. Une lambda qui incrémente un compteur externe ou écrit dans un journal peut
s'exécuter zéro fois, une fois, ou plusieurs fois par élément. Gardez-les pures : entrée vers sortie,
sans état modifié.

**Une closure capture la variable, pas sa valeur.** C'est la source d'un bug classique : une lambda
créée dans une boucle et exécutée après voit la valeur **finale** de la variable capturée. En C#
moderne, la variable d'itération d'un `foreach` est capturée par tour, ce qui règle le cas courant ;
une variable de boucle `for` déclarée à l'extérieur, en revanche, reste partagée. Le remède est de
copier la valeur dans une variable locale au tour.

## Exemple commenté

Une méthode générique dont l'algorithme ne dépend pas du type, et dont le critère est fourni par
l'appelant :

```csharp
public static IReadOnlyList<T> Filter<T>(IReadOnlyList<T> source, Func<T, bool> keep)
{
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(keep);

    var kept = new List<T>();
    foreach (T item in source)
    {
        // Le prédicat est appelé exactement une fois par élément : contrat explicite.
        if (keep(item))
        {
            kept.Add(item);
        }
    }

    return kept;
}
```

Aucune contrainte n'est déclarée, parce que le corps n'a besoin d'aucune capacité particulière de `T`.
Le paramètre s'appelle `keep` et non `f` : l'appel se lit comme une phrase.

Une variante qui exige une capacité, avec la contrainte minimale correspondante :

```csharp
public static T MaxOrThrow<T>(IReadOnlyList<T> source) where T : IComparable<T>
{
    ArgumentNullException.ThrowIfNull(source);
    if (source.Count == 0)
    {
        // Aucun maximum n'existe : inventer une valeur serait un mensonge.
        throw new InvalidOperationException("La collection est vide.");
    }

    T best = source[0];
    for (int index = 1; index < source.Count; index++)
    {
        if (source[index].CompareTo(best) > 0)
        {
            best = source[index];
        }
    }

    return best;
}
```

## Contre-exemple et erreur fréquente

```csharp
public static object[] Filter(object[] source, Func<object, bool> keep)
{
    var kept = new List<object>();
    foreach (object item in source)
    {
        if (keep(item)) { kept.Add(item); }
    }

    return kept.ToArray();
}

// À l'appel, le type est perdu et doit être rétabli à la main.
object[] result = Filter(numbers, o => (int)o > 10);
int first = (int)result[0];
```

Le type disparaît à l'entrée. Chaque `int` placé dans un `object` est encapsulé dans un objet du tas :
sur un million d'éléments, c'est un million d'allocations évitables. Et le transtypage `(int)o` est
une supposition non vérifiée : le jour où la collection contient une chaîne, l'`InvalidCastException`
survient à l'exécution, loin de la ligne qui a introduit l'erreur.

La version générique règle les trois problèmes d'un coup, sans ligne supplémentaire.

Second piège, dans une boucle :

```csharp
var actions = new List<Action>();
for (int i = 0; i < 3; i++)
{
    actions.Add(() => Console.WriteLine(i));   // Capture la variable, pas sa valeur.
}

foreach (Action action in actions) { action(); }   // Affiche 3, 3, 3.
```

Les trois lambdas partagent la même variable `i`, dont la valeur finale est `3`. La correction tient
en une ligne : `int copy = i;` à l'intérieur du corps, puis capturer `copy`.

## Vérification de compréhension

Pour une méthode qui retourne le premier élément satisfaisant un critère, dites si un générique est
justifié, quelle contrainte est nécessaire, et ce que le prédicat doit garantir.

:::quiz
id=generics-delegates-001-check
question=Quelle contrainte ajouter à une méthode générique Max dont le corps appelle CompareTo sur T ?
option=Aucune : tout type C# sait se comparer
option=Une contrainte de comparabilité sur T, la capacité minimale que le corps exige réellement
option=Une contrainte de type référence avec constructeur public, pour couvrir tous les cas d'usage
correct=1
success=Correct : on n'ajoute que la contrainte que le corps exige ; toute contrainte supplémentaire restreint les types acceptés sans rien apporter.
retry=Relisez le passage sur les contraintes minimales, et la variante qui accepte un IComparer pour ne rien imposer à l'appelant.
:::

## Exercice guidé

Ouvrez `csharp-generic-maximum-001` dans `/practice`, puis procédez ainsi.

1. Écrivez la signature générique et la contrainte, en justifiant chacune par une ligne du futur corps.
2. Listez les cas : collection vide, un seul élément, doublons du maximum, ordre décroissant.
3. Implémentez, puis vérifiez qu'aucun transtypage n'apparaît dans votre code ni chez l'appelant.
4. Comparez vos prédictions aux résultats.

## Exercice autonome

Écrivez une méthode générique qui regroupe des éléments par clé, la clé étant fournie par l'appelant.

Décidez avant de coder : la signature, la contrainte éventuelle sur le type de clé, le comportement
sur collection vide, le nombre d'appels garanti du sélecteur par élément, et l'ordre des groupes en
sortie. Justifiez chaque décision.

## Débogage

Un ticket indique : « Le journal contient trois fois plus de lignes que d'éléments traités. »

1. **Symptôme** : le volume de journal ne correspond pas au volume de données.
2. **Hypothèse** : une lambda à effet de bord est évaluée plusieurs fois par élément.
3. **Preuve** : incrémentez un compteur dans la lambda et comparez-le au nombre d'éléments, sans
   modifier la logique.
4. **Prévention** : rendez la lambda pure, déplacez l'écriture du journal dans la boucle appelante, et
   ajoutez un test qui compte les invocations du prédicat.

## Entretien

Question posée à voix haute : *quand écrivez-vous une méthode générique plutôt que deux surcharges ?*

Une réponse solide part de l'algorithme : s'il est identique et que seule la donnée change, le
générique évite la duplication. Elle mentionne les contraintes comme expression du besoin réel, et
reconnaît le cas où deux surcharges spécialisées restent plus lisibles.

## Résumé

- Un générique conserve le type, évite le boxing et déplace les erreurs vers la compilation.
- La contrainte se limite à ce que le corps exige.
- Les delegates prédéfinis suffisent presque toujours ; le nom du paramètre porte le sens.
- Une lambda doit rester pure ; les effets de bord dépendent d'un nombre d'appels non garanti.
- Une closure capture la variable, pas sa valeur.

## Cartes de révision

Question : quel coût cache le passage par `object` pour un type valeur ? Réponse attendue : une
allocation de boxing par élément, plus un transtypage non vérifié à la sortie.

Question : pourquoi une lambda ne doit-elle pas modifier un état externe ? Réponse attendue : le
nombre et l'ordre de ses appels ne sont pas garantis, en particulier avec une évaluation différée.

## Test de maîtrise

Sans relire, écrivez la signature d'une méthode générique qui retourne les N plus grands éléments
d'une collection selon un critère fourni. Justifiez la contrainte retenue, précisez le comportement
si N dépasse la taille, et écrivez le test qui prouve que la collection d'entrée est intacte.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
