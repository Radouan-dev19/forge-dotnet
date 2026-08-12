# Reformuler et estimer avant de coder

## Objectif observable

À la fin de cette leçon, vous saurez transformer un énoncé flou en contrat écrit — entrées, sortie,
invariant, cas refusés — et annoncer la complexité d'une solution en comptant ses opérations
dominantes, avant d'avoir écrit une seule ligne.

## Prérequis

- Avoir lu `files-json-001` et savoir traiter séparément absence, illisibilité et contenu fautif.
- Savoir écrire une boucle imbriquée et estimer combien de fois son corps s'exécute.

## Intuition

Un énoncé d'exercice ou un ticket de production sont tous les deux ambigus. La reformulation est
l'acte qui rend l'ambiguïté visible : elle nomme ce qui entre, ce qui sort, ce qui doit rester vrai,
et ce qui doit être refusé.

La complexité, elle, ne mesure pas une durée. Elle mesure comment le nombre d'opérations **croît**
quand l'entrée grandit. C'est pour cela qu'elle se raisonne sur le papier, pas au chronomètre.

## Explication

**Reformuler, c'est écrire quatre lignes.** *Entrée* : nature, bornes, valeurs possibles, y compris
vide et nul. *Sortie* : type, et ce qu'elle vaut quand aucune réponse naturelle n'existe.
*Invariant* : la phrase qui doit rester vraie pendant tout le traitement. *Refusé* : ce qui relève
d'une faute d'appelant plutôt que d'un cas métier.

Ces quatre lignes coûtent trois minutes et suppriment la moitié des reprises. Sur *« retourner la
moyenne des mesures »*, elles font apparaître immédiatement la question qui n'était écrite nulle
part : que vaut la moyenne d'une liste vide ? Zéro serait un mensonge. C'est un refus.

**La complexité se compte, elle ne s'estime pas.** La méthode tient en trois questions. Quelle est la
taille de l'entrée, appelons-la *n* ? Quelle opération se répète le plus ? Combien de fois s'exécute-t-elle
en fonction de *n* ?

Une boucle simple sur la collection donne *n* itérations, donc O(n). Deux boucles imbriquées
parcourant chacune la collection donnent *n × n*, donc O(n²). Une boucle qui divise l'espace de
recherche par deux à chaque tour donne log₂(n) itérations, donc O(log n). Trier puis parcourir donne
O(n log n) — c'est le tri qui domine.

**Les constantes disparaissent, la croissance reste.** O(2n) et O(n) sont la même classe : doubler
le travail par élément ne change pas la façon dont le coût grandit. Cela ne signifie pas que la
constante n'existe pas — sur 100 éléments elle domine tout — mais qu'elle ne décide pas du
comportement à grande échelle. La complexité répond à *« que se passe-t-il si l'entrée est multipliée
par mille ? »*, pas à *« combien de millisecondes ? »*.

Les ordres de grandeur rendent cela concret. Pour *n* = 1 000 000 : O(log n) ≈ 20 opérations,
O(n) = un million, O(n log n) ≈ 20 millions, O(n²) = mille milliards. La dernière ligne n'est pas
« plus lent » : elle est **impossible**. C'est là que le raisonnement paie.

**Le pire cas prime, sauf mention explicite.** Une recherche linéaire trouve parfois au premier essai.
Annoncer O(1) pour cela serait trompeur : la garantie donnée à l'appelant porte sur le pire cas,
O(n). Le cas moyen se mentionne quand il est structurellement différent — c'est notamment vrai des
tables de hachage, vues en `structures-dictionaries-recursion-001`.

**L'espace se compte aussi.** Une solution qui alloue un tableau de taille *n* est en O(n) en espace ;
une solution qui n'utilise que quelques variables est en O(1). Sur des volumes importants, c'est
souvent l'espace qui bloque en premier. Annoncez toujours les deux.

**Le piège des opérations qui semblent gratuites.** `list.Contains(x)` à l'intérieur d'une boucle sur
la même liste transforme un O(n) apparent en O(n²) réel, parce que `Contains` parcourt lui-même la
collection. La règle : toute méthode appelée dans une boucle apporte sa propre complexité, qui se
multiplie. Lire la documentation d'une méthode de la bibliothèque standard fait partie du comptage.

## Exemple commenté

Énoncé brut : *« compte combien de valeurs apparaissent plus d'une fois. »*

La reformulation, écrite avant tout code :

```text
Entrée    : une séquence d'entiers, possiblement vide, non triée, valeurs quelconques.
Sortie    : un entier >= 0, le nombre de valeurs distinctes apparaissant au moins deux fois.
Invariant : l'entrée n'est jamais modifiée.
Refusé    : la référence nulle (faute d'appelant). Une séquence vide retourne 0, ce n'est pas un refus.
```

Deux implémentations correctes, deux complexités très différentes :

```csharp
// Version naïve : pour chaque élément, on recompte toute la collection.
// La boucle externe fait n tours, Count en fait n : O(n²) en temps, O(1) en espace.
public static int CountDuplicatesNaive(IReadOnlyList<int> values)
{
    ArgumentNullException.ThrowIfNull(values);
    var seen = new List<int>();
    int duplicates = 0;
    foreach (int value in values)
    {
        if (values.Count(candidate => candidate == value) > 1 && !seen.Contains(value))
        {
            seen.Add(value);       // Contains parcourt lui aussi : le coût se multiplie encore.
            duplicates++;
        }
    }

    return duplicates;
}

// Version par comptage : un seul parcours, un accès par clé en coût moyen constant.
// O(n) en temps, O(n) en espace — on échange de la mémoire contre du temps.
public static int CountDuplicates(IReadOnlyList<int> values)
{
    ArgumentNullException.ThrowIfNull(values);
    var occurrences = new Dictionary<int, int>();
    foreach (int value in values)
    {
        occurrences[value] = occurrences.GetValueOrDefault(value) + 1;
    }

    return occurrences.Count(entry => entry.Value > 1);
}
```

Sur 1 000 éléments, la première fait environ un million d'opérations et la seconde mille. Les deux
donnent le même résultat : seule la reformulation puis le comptage permettent de choisir.

## Contre-exemple et erreur fréquente

L'erreur la plus coûteuse n'est pas de mal compter, c'est de compter la mauvaise chose :

```csharp
public static bool HasPair(int[] values, int target)
{
    for (int i = 0; i < values.Length; i++)
    {
        // « Une seule boucle visible, donc O(n) » — faux : Contains parcourt la collection.
        if (values.Contains(target - values[i]))
        {
            return true;
        }
    }

    return false;
}
```

L'auteur annoncera O(n) parce qu'il ne voit qu'une boucle. Le code est en réalité O(n²) : `Contains`
effectue son propre parcours à chaque tour. Sur 100 éléments personne ne s'en aperçoit ; sur 100 000
la requête expire, et le diagnostic se fait sous pression.

Second défaut, plus subtil : la méthode retourne `true` si la valeur trouvée est l'élément courant
lui-même. Pour `values = [4]` et `target = 8`, elle répond `true` alors qu'il n'existe aucune
**paire**. La reformulation aurait posé la question — *deux éléments distincts, ou deux positions
distinctes ?* — avant que le bug n'existe.

## Vérification de compréhension

Pour *« trouver les deux valeurs dont la somme vaut une cible »*, écrivez les quatre lignes de
reformulation, puis annoncez la complexité en temps et en espace de votre approche.

:::quiz
id=algo-reformulation-complexity-001-check
question=Une boucle unique sur une collection de taille n appelle Contains sur cette même collection à chaque tour. Quelle est la complexité en temps ?
option=O(n), car il n'y a qu'une seule boucle écrite dans le code
option=O(n au carré), car chaque appel à Contains effectue son propre parcours de la collection
option=O(log n), car la recherche s'arrête dès qu'elle trouve
correct=1
success=Correct : toute méthode appelée dans une boucle apporte sa propre complexité, et les deux se multiplient. Compter les boucles visibles ne suffit pas.
retry=Relisez le passage sur les opérations qui semblent gratuites, puis recomptez le nombre total de comparaisons effectuées.
:::

## Exercice guidé

Ouvrez `algo-unique-count-001` dans `/practice`, puis procédez ainsi.

1. Écrivez les quatre lignes de reformulation avant tout code, y compris le cas de la collection vide.
2. Proposez deux approches et annoncez pour chacune la complexité en temps **et** en espace.
3. Implémentez celle que vous avez choisie, et justifiez le compromis en une phrase.
4. Vérifiez votre annonce en comptant les opérations sur une entrée de trois éléments.

## Exercice autonome

Énoncé volontairement flou : *« retourne le produit le plus vendu. »*

Écrivez la reformulation complète. Elle doit trancher au minimum : ce qui se passe en cas d'égalité,
ce que retourne un catalogue vide, si « vendu » compte les quantités ou les commandes, et si les
commandes annulées comptent. Annoncez ensuite la complexité de votre approche.

## Débogage

Un ticket indique : « L'export fonctionne en recette et expire en production. »

1. **Symptôme** : le comportement dépend du volume, pas de la donnée.
2. **Hypothèse** : un traitement quadratique, invisible sur le jeu de recette.
3. **Preuve** : mesurez le temps sur 100, 1 000 puis 10 000 lignes. Un facteur dix sur l'entrée qui
   produit un facteur cent sur la durée confirme un O(n²).
4. **Prévention** : notez la complexité dans le commentaire de la méthode, et ajoutez un test sur un
   volume représentatif de la production, pas de la recette.

## Entretien

Question posée à voix haute : *comment estimez-vous le coût d'un algorithme que vous venez d'écrire ?*

Une réponse solide identifie l'opération dominante, la compte en fonction de la taille d'entrée,
annonce temps **et** espace, et précise s'il s'agit du pire cas ou du cas moyen. Une réponse faible
cite une classe de complexité sans jamais dire ce qui a été compté.

## Résumé

- Reformuler, c'est écrire entrée, sortie, invariant et refusé — avant le code.
- La complexité mesure une croissance, pas une durée.
- Les constantes disparaissent ; l'ordre de grandeur décide de ce qui est possible.
- Le pire cas prime, sauf mention explicite du cas moyen.
- Toute méthode appelée dans une boucle apporte sa propre complexité.

## Cartes de révision

Question : que valent respectivement O(log n), O(n) et O(n²) pour un million d'éléments ? Réponse
attendue : environ vingt opérations, un million, et mille milliards — la dernière est impraticable.

Question : pourquoi annoncer aussi la complexité en espace ? Réponse attendue : sur de gros volumes,
c'est souvent la mémoire qui bloque avant le temps.

## Test de maîtrise

Sans relire, reformulez *« détecter si une séquence contient un doublon »* en quatre lignes, proposez
deux implémentations de complexités différentes, annoncez temps et espace pour chacune, et expliquez
dans quelle situation vous choisiriez la plus lente.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
