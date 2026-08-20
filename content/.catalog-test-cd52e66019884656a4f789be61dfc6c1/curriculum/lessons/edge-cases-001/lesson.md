# Concevoir les cas limites avant la boucle

## Objectif observable

À la fin de cette leçon, vous saurez produire, avant d'écrire une ligne d'implémentation, une table
de cas couvrant le vide, les bornes, les doublons et l'absence, et vous saurez reconnaître un test
qui ne pourrait jamais échouer.

## Prérequis

- Avoir lu `strings-dates-001` et savoir rendre un calcul indépendant de la machine.
- Savoir écrire une boucle et une condition composée.

## Intuition

Les cas limites ne sont pas des exceptions à traiter après coup : ce sont des morceaux du contrat que
personne n'a écrits. Un programme qui « marche sauf quand la liste est vide » n'a pas un bug, il a un
contrat incomplet.

Écrire la table des cas **avant** la boucle change l'ordre des découvertes : les ambiguïtés
apparaissent pendant qu'elles coûtent une phrase, pas pendant qu'elles coûtent une correction en
production.

## Explication

**Quatre familles couvrent l'essentiel.** Le *vide* — collection sans élément, chaîne vide, intervalle
nul. Les *bornes* — la valeur exactement au seuil, et ses deux voisines immédiates. Les *doublons* —
deux éléments égaux, et la question de savoir si l'ordre entre eux compte. L'*absence* — la valeur
recherchée n'existe pas, ou existe mais vaut `null`.

Pour chaque famille, la question n'est pas « que fait mon code ? » mais « qu'est-ce que le métier
attend ? ». La somme d'une collection vide vaut zéro, ce qui est raisonnable. Le **maximum** d'une
collection vide n'a pas de réponse naturelle : retourner zéro serait un mensonge, et c'est
exactement pour cela que `Enumerable.Max()` lève une exception plutôt que d'inventer une valeur.

**La borne se teste par trois.** Pour un seuil à 50, testez 49, 50 et 51. C'est la seule façon de
distinguer `>` de `>=`, et c'est la faute la plus fréquente en revue de code. Une suite qui ne
contient que 10 et 100 laisse passer les deux implémentations.

**Un test qui ne peut pas échouer ne prouve rien.** C'est le critère le plus utile de la leçon. Avant
d'ajouter un cas, demandez-vous : *quelle implémentation plausible mais fausse ce test
réfuterait-il ?* Si la réponse est « aucune », le cas est décoratif. Un test qui vérifie
`Sum([1,2,3]) == 6` ne réfute pas une implémentation qui ignore les nombres négatifs ; un test qui
vérifie `Sum([1,-2,3]) == 2` le réfute.

C'est aussi la parade contre la réponse codée en dur. Si vos exemples visibles sont les seuls cas
testés, une implémentation qui retourne la bonne constante passe. Les cas cachés de Forge.NET existent
pour cette raison : ils font varier valeurs, tailles et bornes.

**La table précède le code, et se garde.** Écrite dans le manifeste de réflexion, elle sert trois
fois : à clarifier le contrat avant l'implémentation, à guider la suite de tests, puis à documenter
la décision six mois plus tard. Les cas qu'on choisit de **ne pas** traiter méritent d'y figurer
explicitement, avec la raison.

**Distinguer refus et valeur.** Une entrée hors contrat — quantité négative, référence nulle — se
refuse par une exception : c'est l'appelant qui est fautif. Une absence attendue — aucun résultat
correspondant — se modélise par une valeur : collection vide, `null` documenté, ou type option maison.
Confondre les deux produit soit des exceptions dans le chemin nominal, soit des données inventées.

## Exemple commenté

Une table de cas écrite avant l'implémentation, pour *« retourner l'indice du premier élément
strictement supérieur au seuil, ou -1 »* :

```text
| Cas                    | Entrée                    | Attendu | Ce que le cas réfute
|------------------------|---------------------------|---------|-----------------------------------
| Vide                   | [], seuil 5               | -1      | un accès à values[0] sans garde
| Aucun au-dessus        | [1,2,3], seuil 5          | -1      | un retour de 0 par défaut
| Borne exacte           | [5], seuil 5              | -1      | l'usage de >= au lieu de >
| Juste au-dessus        | [6], seuil 5              | 0       | l'usage de > au lieu de >=
| Premier de deux        | [9,7], seuil 5            | 0       | un parcours qui retourne le dernier
| Doublons               | [7,7], seuil 5            | 0       | un retour de l'indice le plus grand
```

L'implémentation devient mécanique une fois la table écrite :

```csharp
public static int FirstIndexAbove(IReadOnlyList<int> values, int threshold)
{
    ArgumentNullException.ThrowIfNull(values);

    for (int index = 0; index < values.Count; index++)
    {
        if (values[index] > threshold)   // Strictement : la borne exacte ne compte pas.
        {
            return index;
        }
    }

    return -1;   // Absence attendue : une valeur, pas une exception.
}
```

La colonne « ce que le cas réfute » est la partie utile : elle transforme une liste de cas en preuve.

## Contre-exemple et erreur fréquente

```csharp
[Fact]
public void FirstIndexAbove_Works()
{
    Assert.Equal(0, FirstIndexAbove(new[] { 9, 7 }, 5));
    Assert.Equal(0, FirstIndexAbove(new[] { 8, 6 }, 5));
    Assert.Equal(0, FirstIndexAbove(new[] { 7, 7 }, 5));
}
```

Ce test est vert et ne prouve presque rien. Les trois cas partagent la même réponse attendue, `0` :
une implémentation qui retourne systématiquement `0` les passe tous. Ni le vide, ni l'absence, ni la
borne exacte ne sont couverts, donc la confusion entre `>` et `>=` reste invisible.

Le défaut est structurel, pas quantitatif : ajouter dix cas de la même famille n'améliorerait rien.
Ce qui manque, c'est la variété des **réponses attendues** et au moins un cas qui réfute chaque
erreur plausible.

## Vérification de compréhension

Pour une méthode qui calcule une moyenne, énoncez la réponse attendue sur collection vide, et
justifiez pourquoi c'est un refus ou une valeur.

:::quiz
id=edge-cases-001-check
question=Quel critère permet de juger qu'un cas de test mérite d'être ajouté ?
option=Il augmente le pourcentage de lignes couvertes par la suite
option=Il réfute une implémentation plausible mais fausse que les autres cas laisseraient passer
option=Il utilise des valeurs différentes de celles déjà employées
correct=1
success=Correct : la valeur d'un cas se mesure à l'erreur qu'il rend impossible, pas au nombre de lignes qu'il traverse ni à la nouveauté de ses données.
retry=Relisez le passage « un test qui ne peut pas échouer ne prouve rien » et la colonne « ce que le cas réfute » de la table d'exemple.
:::

## Exercice guidé

Ouvrez `csharp-clamp-value-001` dans `/practice`, puis procédez ainsi.

1. Écrivez la table des cas avec une colonne « ce que le cas réfute », sans écrire de code.
2. Vérifiez que deux cas au moins ont des réponses attendues différentes.
3. Implémentez, puis exécutez.
4. Pour chaque cas qui passe du premier coup, demandez-vous s'il aurait pu échouer.

## Exercice autonome

Une règle : *un code postal français est valide s'il contient exactement cinq chiffres et ne commence
pas par 00.*

Écrivez la table des cas avant tout code. Couvrez au minimum : chaîne vide, quatre chiffres, six
chiffres, présence d'une lettre, espaces autour, et les deux voisins de la borne interdite. Indiquez
pour chacun ce qu'il réfute.

## Débogage

Un ticket indique : « La remise de fidélité ne s'applique pas au client qui atteint exactement le
seuil. »

1. **Symptôme** : le seuil exact est traité comme en dessous du seuil.
2. **Hypothèse** : la comparaison utilise `>` là où le métier attend `>=`.
3. **Preuve** : exécutez les trois valeurs voisines du seuil et comparez ; la divergence porte sur une
   seule des trois.
4. **Prévention** : ajoutez les trois cas de borne à la suite de tests. Le cas exact aurait échoué
   avant la correction — c'est ce qui en fait une preuve.

## Entretien

Question posée à voix haute : *comment décidez-vous que vous avez assez de tests ?*

Une réponse solide ne cite pas un taux de couverture. Elle décrit un raisonnement par erreurs
plausibles : quelles implémentations fausses pourraient encore passer, et quel cas les éliminerait.
Elle reconnaît aussi les cas volontairement non traités et la raison de ce choix.

## Résumé

- Vide, borne, doublon et absence font partie du contrat, pas des corrections tardives.
- Une borne se teste par trois valeurs : en dessous, exacte, au-dessus.
- Un cas utile réfute une implémentation plausible mais fausse.
- Un refus est une exception ; une absence attendue est une valeur.
- La table des cas s'écrit avant le code et se conserve après.

## Cartes de révision

Question : pourquoi le maximum d'une collection vide lève-t-il une exception plutôt que de retourner
zéro ? Réponse attendue : aucune valeur n'est vraie, et en inventer une propagerait une donnée fausse.

Question : quelle colonne transforme une liste de cas en preuve ? Réponse attendue : celle qui nomme
l'erreur plausible que chaque cas rend impossible.

## Test de maîtrise

Sans relire, écrivez la table des cas d'une méthode qui découpe une facture en échéances mensuelles
égales, le dernier versement absorbant l'arrondi. Couvrez le montant nul, une seule échéance, un
montant non divisible et un nombre d'échéances négatif. Indiquez pour chaque cas ce qu'il réfute.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
