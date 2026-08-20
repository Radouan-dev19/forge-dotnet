# Filet de non-régression et refactoring

## Objectif observable

À la fin de cette leçon, vous saurez établir un filet de tests **avant** de modifier du code existant,
distinguer un refactoring d'un changement de comportement, et reconnaître les trois erreurs qui
transforment un nettoyage en régression.

## Prérequis

- Avoir lu `tests-api-factory-001` et savoir tester une application par ses points d'entrée.
- Avoir lu `tests-domain-rules-001` et savoir écrire un test qui survit aux réécritures.

## Intuition

Refactorer, c'est **changer la structure sans changer le comportement**. La définition contient sa
propre exigence : si vous ne pouvez pas prouver que le comportement n'a pas changé, vous ne refactorez
pas — vous réécrivez et vous espérez.

Le filet est ce qui rend la promesse vérifiable. Il s'écrit avant, sur le code tel qu'il est, y compris
sur ses bizarreries.

## Explication

**Le filet se pose avant, sur le comportement actuel.** Y compris quand ce comportement paraît faux.
Un code qui arrondit à l'envers depuis trois ans a peut-être des appelants qui en dépendent. Le filet
capture ce qui *est* ; corriger ce qui *devrait être* est un second geste, séparé, avec ses propres
tests.

Mélanger les deux est l'erreur la plus fréquente : quand un test échoue, on ne sait plus si c'est le
refactoring qui a cassé quelque chose ou la correction qui a fait son travail.

**Le filet porte sur l'observable, jamais sur la structure.** Un test qui vérifie l'ordre des appels
internes échouera au premier déplacement de code — et il échouera sans qu'aucun comportement n'ait
changé, ce qui le rend inutile comme filet. C'est la ligne tracée dans `tests-domain-rules-001`.

Là où le code existant n'est pas testable en l'état, l'ordre est : caractériser par le niveau
accessible — souvent le point d'entrée HTTP de `tests-api-factory-001` — puis extraire la règle, puis
la tester finement.

**Les tests de caractérisation sont légitimes.** Face à du code sans spécification, on exécute, on
observe, on fige le résultat observé dans un test. Ce test ne dit pas « c'est correct » : il dit
« c'était ceci avant ». C'est exactement ce dont on a besoin pour refactorer sans rien casser.

**Un pas à la fois, vert entre chaque pas.** Extraire une méthode, exécuter la suite. Renommer,
exécuter la suite. Déplacer, exécuter la suite. Enchaîner cinq transformations puis lancer les tests
transforme un échec en enquête. La discipline coûte quelques secondes par pas et fait gagner des
heures.

**Les transformations à faible risque d'abord.** Renommer, extraire une méthode, remplacer un nombre
magique par une constante nommée, réduire la portée d'une variable, inverser une condition pour
supprimer un niveau d'imbrication. Chacune est mécanique et vérifiable. Les transformations à risque —
changer une signature publique, modifier une structure de données, remplacer un algorithme — demandent
un filet plus dense.

**Trois erreurs qui font régresser.** *Changer le comportement en croyant nettoyer* : remplacer une
comparaison stricte par une comparaison large « parce que c'est plus logique » déplace une frontière.
*Élargir une garde* : transformer un refus en valeur par défaut fait disparaître une erreur qui
protégeait quelque chose. *Supprimer du code « mort »* qui ne l'était pas : une branche apparemment
inaccessible peut être atteinte par une entrée que vous n'avez pas imaginée.

**Le refactoring se commet séparément.** Un commit qui mélange restructuration et correction est
illisible en revue : le diff est énorme et la modification de comportement s'y noie. Séparer les deux
est le sujet de `git-commits-history-001`, et c'est ce qui rend la revue de
`quality-review-diffs-001` possible.

**Savoir s'arrêter.** Tout code n'a pas besoin d'être parfait. Le refactoring se justifie quand il rend
un changement prévu plus sûr ou plus rapide, pas quand il satisfait un goût personnel. Du code laid,
stable et couvert peut légitimement rester tel quel.

## Exemple commenté

Le filet posé sur une borne, avant toute modification :

```csharp
public static bool IsIndexValid(int index, int length)
{
    // Deux frontières et deux refus. Ce sont ces quatre cas qui empêcheront
    // une future simplification de déplacer la borne sans qu'on le voie.
    return length > 0 && index >= 0 && index < length;
}
```

```csharp
[Theory]
[InlineData(-1, 3, false)]   // sous la borne basse
[InlineData(0, 3, true)]     // la borne basse exacte
[InlineData(2, 3, true)]     // la borne haute exacte, strictement inférieure à la longueur
[InlineData(3, 3, false)]    // juste au-dessus : l'erreur classique de un
[InlineData(0, 0, false)]    // longueur nulle : aucun index n'est valide
public void Index_SelonLaBorne_EstValideOuNon(int index, int length, bool expected) =>
    Assert.Equal(expected, Guards.IsIndexValid(index, length));
```

La transformation à faible risque, appliquée en un pas :

```csharp
// Avant : trois niveaux d'imbrication, la règle est noyée.
public static decimal Fee(Order? order)
{
    if (order is not null)
    {
        if (order.Lines.Count > 0)
        {
            if (order.Total >= 100m)
            {
                return 0m;
            }
        }
    }

    return 4.9m;
}
```

```csharp
// Après : sorties anticipées, un seul niveau. Le comportement est identique
// pour toutes les entrées — c'est ce que le filet vérifie, pas la lecture.
public static decimal Fee(Order? order)
{
    if (order is null || order.Lines.Count == 0)
    {
        return 4.9m;
    }

    return order.Total >= 100m ? 0m : 4.9m;
}
```

## Contre-exemple et erreur fréquente

```csharp
// Avant : la règle refuse une valeur absente, et le seuil est strict.
public static decimal Fee(Order? order)
{
    ArgumentNullException.ThrowIfNull(order);
    return order.Total > 100m ? 0m : 4.9m;
}
```

```csharp
// « Nettoyage » : trois changements de comportement glissés dans un seul commit
// intitulé « refactoring : simplification du calcul de frais ».
public static decimal Fee(Order? order)
{
    // 1. Le refus devient une valeur par défaut : une erreur d'appelant
    //    disparaît silencieusement et remontera bien plus loin.
    if (order is null)
    {
        return 0m;
    }

    // 2. La comparaison stricte devient large : la commande à exactement
    //    100 euros bascule de payante à gratuite.
    // 3. Le montant du frais change au passage.
    return order.Total >= 100m ? 0m : 5.9m;
}
```

Trois régressions, aucune visible dans le titre du commit.

Le premier changement supprime une garde. L'appel avec une valeur absente ne lève plus rien : le défaut
se manifestera plus tard, dans un total faux, sans trace de sa cause. Une erreur bruyante vaut mieux
qu'un résultat silencieusement faux.

Le deuxième déplace une frontière. Sans le triplet de valeurs autour du seuil — quatre-vingt-dix-neuf,
cent, cent un — rien ne l'aurait détecté, et le chiffre d'affaires bouge.

Le troisième est un changement de règle métier présenté comme un nettoyage. Il ne devrait même pas
être dans ce commit.

La correction : poser d'abord le filet sur le comportement actuel, faire le refactoring seul et le
commettre, puis traiter chaque changement de règle dans son propre commit avec ses propres tests.

## Vérification de compréhension

Vous devez modifier une méthode de six cents lignes sans aucun test. Décrivez vos trois premiers
gestes, dans l'ordre, et dites ce que vous vous interdisez tant que le filet n'est pas en place.

:::quiz
id=quality-regression-refactoring-001-check
question=Pourquoi écrire le filet sur le comportement actuel, même quand ce comportement paraît faux ?
option=Parce qu'un test sur le comportement correct serait plus long à écrire
option=Parce que le filet doit prouver que le refactoring n'a rien changé : y mêler une correction rend tout échec ambigu, et des appelants peuvent dépendre du comportement actuel
option=Parce que les tests de caractérisation ne peuvent pas exprimer un comportement correct
correct=1
success=Correct : figer ce qui est, refactorer, puis corriger dans un second geste séparé avec ses propres tests.
retry=Relisez le passage sur le filet posé avant, et demandez-vous ce que signifie un test rouge quand refactoring et correction sont mêlés.
:::

## Exercice guidé

Ouvrez `quality-regression-bounds-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, les cinq cas qui encadrent la validité d'un index, longueur nulle
   comprise.
2. Implémentez la règle en exprimant les deux bornes explicitement.
3. Vérifiez la valeur exactement égale à la longueur : c'est l'erreur de un la plus courante.
4. Enchaînez avec `quality-null-guard-001`, qui traite l'absence avant toute déréférence.

## Exercice autonome

Prenez une méthode existante de votre code, ou l'une de celles du laboratoire
`content/labs/api-mini-erp/`, comportant au moins trois niveaux d'imbrication.

Posez le filet — listez les comportements observables, écrivez les tests — puis appliquez trois
transformations à faible risque, une par une, en exécutant la suite entre chacune. Notez ce que le
filet a détecté, et ce qu'il aurait laissé passer.

## Débogage

Un ticket indique : « Depuis le nettoyage de la semaine dernière, certains clients ne paient plus les
frais de port. »

1. **Symptôme** : un changement de comportement métier suit un commit annoncé comme sans effet.
2. **Hypothèse** : une comparaison a changé de stricte à large, ou une garde a été élargie.
3. **Preuve** : comparez le diff du commit ligne à ligne, en cherchant les opérateurs de comparaison et
   les gardes supprimées.
4. **Prévention** : exiger un filet avant tout refactoring, et refuser en revue tout commit qui mêle
   restructuration et changement de règle.

## Entretien

Question posée à voix haute : *comment abordez-vous du code existant que vous devez modifier et qui
n'a aucun test ?*

Une réponse solide commence par caractériser le comportement actuel au niveau accessible, refuse de
corriger avant d'avoir le filet, avance par petits pas vérifiés, et sait dire que tout code n'a pas
besoin d'être refactoré.

## Résumé

- Refactorer, c'est changer la structure en prouvant que le comportement tient.
- Le filet se pose avant, sur ce qui est, pas sur ce qui devrait être.
- Un pas, une exécution de la suite : enchaîner transforme un échec en enquête.
- Garde élargie, comparaison assouplie et code « mort » supprimé sont les trois pièges.
- Restructuration et correction ne partagent jamais un commit.

## Cartes de révision

Question : que dit exactement un test de caractérisation ? Réponse attendue : « c'était ceci avant »,
et non « c'est correct ».

Question : quand un refactoring se justifie-t-il ? Réponse attendue : quand il rend un changement
prévu plus sûr ou plus rapide, pas quand il satisfait un goût.

## Test de maîtrise

Sans relire, décrivez la reprise complète d'un module hérité sans tests : la façon de caractériser son
comportement, l'ordre des transformations, le rythme de vérification, la séparation entre
restructuration et correction, le découpage en commits, et les trois erreurs que vous surveillez
particulièrement.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
