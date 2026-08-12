# Recherche linéaire et binaire

## Objectif observable

À la fin de cette leçon, vous saurez écrire une recherche binaire correcte du premier coup en
énonçant son invariant d'intervalle, et vous saurez dire à quelle condition elle est applicable — et
à quel moment la recherche linéaire reste le bon choix.

## Prérequis

- Avoir lu `algo-reformulation-complexity-001` et savoir compter les opérations dominantes.
- Savoir manipuler des indices de tableau et écrire une boucle `while`.

## Intuition

La recherche linéaire regarde chaque élément jusqu'à trouver. Elle n'exige rien de la séquence, et
c'est sa force : elle marche toujours.

La recherche binaire, elle, achète une vitesse considérable au prix d'une exigence forte — la
séquence doit être triée selon le même critère que la recherche. Elle divise l'espace restant par
deux à chaque tour, ce qui ramène un million d'éléments à vingt comparaisons.

## Explication

**Le bon réflexe est de vérifier la précondition avant tout.** Une recherche binaire sur une séquence
non triée ne « fonctionne pas mal » : elle retourne des résultats faux, sans erreur, de façon
reproductible. C'est le pire genre de bug — silencieux et déterministe. Avant d'écrire la moindre
ligne, posez la question : *cette collection est-elle triée, et selon quel ordre ?*

**L'invariant est ce qui rend l'algorithme démontrable.** Avec un intervalle fermé `[left, right]`,
l'invariant s'énonce ainsi : *si la cible est présente, alors son indice est compris entre `left` et
`right` inclus.* Chaque tour doit préserver cette phrase. Quand `left` dépasse `right`, l'intervalle
est vide, et l'invariant garantit que la cible n'existe pas.

Tout se déduit de là. On compare l'élément du milieu à la cible. S'il est plus petit, la cible ne peut
être que **strictement après** : `left = middle + 1`. S'il est plus grand, elle ne peut être que
strictement avant : `right = middle - 1`. Le `+1` et le `-1` ne sont pas des ajustements empiriques :
ils viennent du fait que `middle` a déjà été testé, donc l'exclure préserve l'invariant.

**Deux fautes classiques, toutes deux mécaniques.** La première est d'écrire `left = middle` au lieu
de `left = middle + 1` : l'intervalle ne rétrécit plus quand il ne reste que deux éléments, et la
boucle tourne indéfiniment. La seconde est de calculer le milieu par `(left + right) / 2` : sur de
très grands indices, la somme dépasse la capacité de l'entier et devient négative. La forme correcte
est `left + (right - left) / 2`, qui ne dépasse jamais.

**Choisir sa convention et s'y tenir.** L'intervalle fermé `[left, right]` avec `while (left <= right)`
est le plus lisible pour débuter. La convention semi-ouverte `[left, right)` avec
`while (left < right)` et `right = middle` existe aussi et est parfaitement correcte. Ce qui produit
des bugs, c'est de mélanger les deux — un `<=` d'une convention avec un `right = middle` de l'autre.

**Chercher la position, pas seulement la présence.** En pratique, la question utile est souvent
*« où insérer cette valeur pour conserver l'ordre ? »* plutôt que *« est-elle là ? »*. .NET fournit
`Array.BinarySearch`, qui retourne l'indice s'il trouve, et le **complément binaire** de la position
d'insertion sinon : un résultat négatif `r` signifie que la valeur s'insérerait en `~r`. Cette
convention surprend la première fois, mais elle donne les deux réponses en un seul appel.

**Quand la recherche linéaire gagne.** Sur une petite collection — quelques dizaines d'éléments — la
recherche linéaire est souvent plus rapide en pratique, parce qu'elle parcourt la mémoire de façon
contiguë et prévisible, là où la binaire saute. Elle gagne aussi dès que la collection change souvent :
maintenir un tri coûte O(n log n), et une seule recherche ne le rentabilise pas. La règle : la binaire
paie quand on cherche **beaucoup de fois** dans une collection **stable**.

## Exemple commenté

```csharp
// Précondition : values est trié par ordre croissant. La méthode ne le vérifie pas —
// le faire coûterait O(n) et annulerait tout l'intérêt de l'algorithme.
public static int BinarySearch(int[] values, int target)
{
    ArgumentNullException.ThrowIfNull(values);

    int left = 0;
    int right = values.Length - 1;

    // Invariant : si target est présent, son indice est dans [left, right].
    while (left <= right)
    {
        // Forme sûre : left + (right - left) / 2 ne dépasse jamais, contrairement à (left + right) / 2.
        int middle = left + (right - left) / 2;

        if (values[middle] == target)
        {
            return middle;
        }

        if (values[middle] < target)
        {
            left = middle + 1;   // middle est déjà testé : l'exclure préserve l'invariant.
        }
        else
        {
            right = middle - 1;
        }
    }

    // Intervalle vide : l'invariant garantit que target est absent.
    return -1;
}
```

Les cas qui prouvent cette implémentation : tableau vide, un seul élément présent, un seul élément
absent, cible au premier indice, cible au dernier indice, cible entre deux valeurs existantes. Six
cas, dont quatre portent sur des bornes — c'est la proportion normale pour un algorithme d'intervalle.

## Contre-exemple et erreur fréquente

```csharp
public static int BinarySearchBroken(int[] values, int target)
{
    int left = 0;
    int right = values.Length - 1;
    while (left <= right)
    {
        int middle = (left + right) / 2;    // Dépassement possible sur de grands indices.
        if (values[middle] == target) { return middle; }
        if (values[middle] < target)
        {
            left = middle;                  // BUG : middle n'est pas exclu.
        }
        else
        {
            right = middle;                 // BUG : idem.
        }
    }

    return -1;
}
```

Ce code est correct sur presque toutes les entrées, ce qui le rend redoutable. Il ne casse que
lorsqu'il reste **exactement deux éléments** : `left = 0`, `right = 1` donne `middle = 0` ; si
l'élément cherché est le plus grand, on écrit `left = 0`, c'est-à-dire la valeur qu'on avait déjà.
L'intervalle ne rétrécit plus et la boucle tourne pour toujours.

Le symptôme en production n'est donc pas un mauvais résultat mais un processeur à 100 % et une requête
qui n'aboutit jamais. Le test qui l'aurait attrapé est le plus simple de tous : `[1, 2]` avec la
cible `2`.

## Vérification de compréhension

Énoncez l'invariant de la recherche binaire en une phrase, puis expliquez pourquoi `middle` doit être
exclu de l'intervalle suivant.

:::quiz
id=algo-search-001-check
question=Pourquoi écrit-on `left + (right - left) / 2` plutôt que `(left + right) / 2` ?
option=Parce que la première forme est plus rapide à l'exécution
option=Parce que la somme de deux grands indices peut dépasser la capacité de l'entier et devenir négative
option=Parce que la division entière arrondit différemment selon la forme employée
correct=1
success=Correct : les deux formes donnent le même milieu, mais seule la seconde évite le dépassement de capacité sur de très grands indices.
retry=Relisez le passage sur les deux fautes classiques : l'une concerne la borne, l'autre le calcul du milieu.
:::

## Exercice guidé

Ouvrez `algo-binary-search-001` dans `/practice`, puis procédez ainsi.

1. Écrivez l'invariant d'intervalle en une phrase avant de coder.
2. Listez les six cas cités dans l'exemple, et prédisez le résultat de chacun.
3. Implémentez avec la convention fermée, sans mélanger les styles.
4. Exécutez `[1, 2]` avec la cible `2` en priorité : c'est le cas qui distingue une implémentation
   correcte d'une boucle infinie.

## Exercice autonome

Écrivez une méthode qui retourne l'indice du **premier** élément supérieur ou égal à une valeur
donnée, ou la longueur du tableau si aucun ne convient — la borne inférieure classique.

Décidez avant de coder : la convention d'intervalle retenue, l'invariant correspondant, le
comportement sur tableau vide, et celui en présence de doublons de la valeur cherchée.

## Débogage

Un ticket indique : « La recherche dans le catalogue ne rend jamais la main sur certaines
références. »

1. **Symptôme** : blocage, pas de résultat erroné, sur une fraction des entrées seulement.
2. **Hypothèse** : l'intervalle cesse de rétrécir dans un cas de bord — probablement à deux éléments.
3. **Preuve** : posez un point d'arrêt conditionnel sur `right - left <= 1` et observez `left`,
   `right` et `middle` sur deux tours consécutifs. Des valeurs identiques confirment l'hypothèse.
4. **Prévention** : ajoutez `[1, 2]` cible `2` et `[1, 2]` cible `1` à la suite de tests, et vérifiez
   dans le code que `middle` est exclu des deux côtés.

## Entretien

Question posée à voix haute : *quand n'utiliseriez-vous pas une recherche binaire ?*

Une réponse solide cite la précondition de tri et son coût, le cas des collections qui changent
souvent, et la taille en dessous de laquelle la linéaire gagne en pratique. Elle mentionne aussi le
danger d'une binaire appliquée à une collection non triée : un résultat faux et silencieux.

## Résumé

- La binaire exige une séquence triée selon le critère recherché, sinon elle ment sans erreur.
- L'invariant d'intervalle rend l'algorithme démontrable et dicte le `+1` et le `-1`.
- `left + (right - left) / 2` évite le dépassement de capacité.
- Une convention se choisit et ne se mélange pas.
- La binaire paie sur une collection stable où l'on cherche souvent.

## Cartes de révision

Question : quel test minimal distingue une recherche binaire correcte d'une boucle infinie ? Réponse
attendue : un tableau de deux éléments où la cible est le plus grand.

Question : que retourne `Array.BinarySearch` quand la valeur est absente ? Réponse attendue : le
complément binaire de la position d'insertion, donc un nombre négatif à retransformer par `~`.

## Test de maîtrise

Sans relire, écrivez une recherche binaire retournant l'indice de la **dernière** occurrence d'une
valeur dans un tableau trié avec doublons. Énoncez l'invariant, justifiez chaque mise à jour de
borne, et donnez les trois cas de test qui prouvent que vous retournez bien la dernière et non la
première.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
