# Tris simples et invariants de boucle

## Objectif observable

À la fin de cette leçon, vous saurez énoncer l'invariant de boucle des tris par sélection, insertion
et à bulles, dire lequel est stable et pourquoi, et justifier qu'on ne les écrit jamais en production
tout en devant savoir les écrire en entretien.

## Prérequis

- Avoir lu `algo-search-001` et savoir énoncer un invariant d'intervalle.
- Savoir échanger deux éléments d'un tableau et écrire une boucle imbriquée.

## Intuition

Un tri simple n'a pas d'intérêt pour trier : `Array.Sort` fait mieux dans tous les cas. Son intérêt
est pédagogique et il est immense : chacun illustre un **invariant de boucle** différent, c'est-à-dire
une phrase vraie à chaque tour, qui prouve que l'algorithme termine et donne le bon résultat.

Savoir énoncer cette phrase, c'est savoir raisonner sur n'importe quelle boucle — y compris celles
que vous écrirez en production.

## Explication

**Sélection.** À chaque tour, on cherche le minimum de la partie non triée et on l'échange avec le
premier élément non trié. L'invariant : *après k tours, les k premières cases contiennent les k plus
petits éléments, dans l'ordre définitif.* Le coût est toujours O(n²) comparaisons, y compris sur un
tableau déjà trié — la recherche du minimum ne peut pas être écourtée. En revanche il n'y a que
n échanges au total, ce qui compte quand un échange est coûteux.

**Insertion.** On parcourt le tableau et on insère chaque élément à sa place dans la partie gauche,
déjà triée. L'invariant : *après k tours, les k premières cases sont triées entre elles, mais pas
nécessairement à leur position finale* — c'est la différence essentielle avec la sélection. Le coût
est O(n²) au pire, mais **O(n) sur un tableau déjà trié**, car la boucle interne s'arrête
immédiatement. C'est pourquoi il reste utilisé en pratique sur de très petits segments, y compris à
l'intérieur d'algorithmes de tri modernes.

**Bulles.** On compare les éléments deux à deux et on échange les couples mal ordonnés, en répétant
jusqu'à ce qu'un passage complet n'effectue aucun échange. L'invariant : *après k passages, les k plus
grands éléments occupent leurs positions finales à droite.* Avec le drapeau d'arrêt anticipé, il
détecte un tableau déjà trié en un seul passage, donc O(n) dans ce cas précis. Il reste le plus lent
des trois en pratique à cause du nombre d'échanges.

**La stabilité est une propriété observable, pas un détail.** Un tri est stable s'il préserve l'ordre
relatif de deux éléments considérés comme égaux par le critère de comparaison. L'insertion et les
bulles sont stables ; la sélection ne l'est pas, parce qu'elle déplace un élément lointain par-dessus
d'autres.

Cela devient concret dès qu'on trie deux fois : trier des commandes par date, puis par client, ne
donne « les commandes de chaque client, par date » **que si le second tri est stable**. Avec un tri
instable, l'ordre des dates est perdu. En .NET, `List<T>.Sort` et `Array.Sort` ne sont **pas** stables ;
`Enumerable.OrderBy` l'est. Ce n'est pas un détail d'implémentation : c'est un contrat documenté sur
lequel s'appuyer.

**Pourquoi on ne les écrit pas en production.** `Array.Sort` utilise un tri hybride en O(n log n) qui
bascule sur l'insertion pour les petits segments et se protège contre les cas dégénérés. Réécrire un
tri revient à produire plus lent et plus risqué. La seule raison légitime : un critère de tri
particulier — et il s'exprime alors par un `IComparer<T>`, pas par un nouvel algorithme.

**Ce qui se transpose vraiment.** L'invariant de boucle n'est pas réservé au tri. Toute boucle non
triviale en possède un, et l'écrire en commentaire au-dessus de la boucle est l'un des gestes qui
distinguent un code relu d'un code écrit. C'est ce que vous garderez de cette leçon dans dix ans.

## Exemple commenté

```csharp
// Tri par insertion. Invariant : à chaque entrée de boucle externe,
// values[0..index-1] est trié entre ses propres éléments.
public static void InsertionSort(int[] values)
{
    ArgumentNullException.ThrowIfNull(values);

    for (int index = 1; index < values.Length; index++)
    {
        int current = values[index];
        int position = index - 1;

        // On décale vers la droite tant que l'élément est plus grand que celui qu'on insère.
        // La comparaison stricte > est ce qui rend le tri STABLE : à valeur égale, on s'arrête,
        // donc l'élément déjà présent reste devant.
        while (position >= 0 && values[position] > current)
        {
            values[position + 1] = values[position];
            position--;
        }

        values[position + 1] = current;
        // Invariant rétabli : values[0..index] est maintenant trié.
    }
}
```

Remplacer `>` par `>=` dans la condition ne change **aucun** résultat sur des entiers — mais rend le
tri instable dès qu'on trie des objets par une clé. C'est un exemple rare où un caractère décide
d'une propriété contractuelle.

Comparaison des trois sur un tableau déjà trié de n éléments :

```text
Sélection : O(n²)  comparaisons, 0 échange utile   — aucun gain, la recherche du minimum est aveugle
Insertion : O(n)   comparaisons, 0 décalage        — la boucle interne s'arrête au premier test
Bulles    : O(n)   comparaisons, 0 échange         — à condition d'avoir le drapeau d'arrêt anticipé
```

## Contre-exemple et erreur fréquente

```csharp
public static void BubbleSortBroken(int[] values)
{
    for (int i = 0; i < values.Length; i++)
    {
        for (int j = 0; j < values.Length - 1; j++)
        {
            if (values[j] > values[j + 1])
            {
                // Échange par addition et soustraction : « astuce » sans variable temporaire.
                values[j] = values[j] + values[j + 1];
                values[j + 1] = values[j] - values[j + 1];
                values[j] = values[j] - values[j + 1];
            }
        }
    }
}
```

Le résultat est trié, et pourtant trois choses ne vont pas.

L'échange arithmétique **dépasse la capacité de l'entier** dès que deux valeurs voisines sont
grandes : `values[j] + values[j + 1]` peut devenir négatif, et le tri produit alors des valeurs
fausses sans lever d'exception. L'astuce n'économise rien : le compilateur gère parfaitement une
variable temporaire, et `(values[j], values[j + 1]) = (values[j + 1], values[j])` fait la même chose
lisiblement.

Ensuite, aucun drapeau d'arrêt : un tableau déjà trié coûte quand même n² comparaisons.

Enfin, la boucle interne ne décroît pas. Après k passages, les k derniers éléments sont déjà à leur
place définitive — les recomparer est du travail garanti inutile. La borne correcte est
`values.Length - 1 - i`, et c'est l'invariant qui le dit.

## Vérification de compréhension

Énoncez l'invariant du tri par sélection, puis expliquez pourquoi il empêche ce tri d'être stable.

:::quiz
id=algo-simple-sorts-001-check
question=Vous triez des commandes par date, puis vous retriez le résultat par client. Que faut-il pour que les commandes de chaque client restent ordonnées par date ?
option=Rien de particulier : deux tris successifs conservent toujours l'ordre du premier
option=Que le second tri soit stable, c'est-à-dire qu'il préserve l'ordre relatif des éléments jugés égaux
option=Trier une seule fois avec une comparaison sur la date uniquement
correct=1
success=Correct : seul un tri stable préserve l'ordre issu du tri précédent entre éléments de même clé. OrderBy est stable, Array.Sort ne l'est pas.
retry=Relisez le passage sur la stabilité : la question n'est pas le résultat d'un tri isolé, mais ce qu'il advient de l'ordre établi par le tri précédent.
:::

## Exercice guidé

Ouvrez `algo-insertion-sort-001` dans `/practice`, puis procédez ainsi.

1. Écrivez l'invariant de la boucle externe en commentaire avant d'implémenter.
2. Implémentez, puis vérifiez que la comparaison retenue préserve la stabilité.
3. Comptez les comparaisons sur un tableau déjà trié et sur un tableau inversé.
4. Comparez vos deux comptages à l'ordre de grandeur annoncé.

## Exercice autonome

Écrivez un tri par sélection, puis modifiez-le pour trier par ordre décroissant sans dupliquer le
code — en injectant la comparaison.

Décidez avant de coder : la signature qui reçoit le critère, le comportement sur tableau vide ou à un
élément, et si votre méthode modifie l'entrée ou en produit une copie. Justifiez ce dernier choix.

## Débogage

Un ticket indique : « Le classement affiche les ex æquo dans un ordre différent à chaque
rafraîchissement. »

1. **Symptôme** : l'instabilité ne concerne que les éléments de score identique.
2. **Hypothèse** : le tri employé n'est pas stable, ou le critère ne départage pas les ex æquo.
3. **Preuve** : construisez un jeu de trois éléments de même score et triez-le deux fois de suite ;
   un ordre différent confirme l'hypothèse.
4. **Prévention** : soit basculer sur un tri stable, soit ajouter un critère de départage
   déterministe — par exemple l'identifiant — et ajouter un test sur des ex æquo.

## Entretien

Question posée à voix haute : *implémentez un tri par insertion et prouvez qu'il termine.*

Une réponse solide écrit l'invariant avant le code, montre que la boucle interne décroît strictement
donc qu'elle termine, et sait dire quel est le meilleur cas et pourquoi. Elle précise aussi qu'en
production elle appellerait `Array.Sort`.

## Résumé

- Chaque tri simple illustre un invariant de boucle différent.
- Sélection : positions définitives, instable, O(n²) même si trié.
- Insertion : préfixe trié entre lui-même, stable, O(n) si déjà trié.
- Bulles : plus grands à droite, stable, O(n) avec arrêt anticipé.
- La stabilité est un contrat : `OrderBy` l'offre, `Array.Sort` non.

## Cartes de révision

Question : quelle différence entre l'invariant de la sélection et celui de l'insertion ? Réponse
attendue : la sélection place des éléments à leur position finale, l'insertion maintient un préfixe
trié entre ses propres éléments.

Question : pourquoi éviter l'échange par addition et soustraction ? Réponse attendue : il dépasse la
capacité de l'entier sur de grandes valeurs, et n'économise rien de réel.

## Test de maîtrise

Sans relire, écrivez un tri à bulles avec arrêt anticipé et borne interne décroissante. Énoncez
l'invariant qui justifie cette borne, donnez la complexité dans le meilleur et le pire cas, et
écrivez le test qui prouve que votre tri est stable sur des objets à clé identique.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
