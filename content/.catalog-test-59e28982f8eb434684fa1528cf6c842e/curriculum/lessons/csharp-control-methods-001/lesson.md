# Contrôle et méthodes prévisibles

## Objectif observable

À la fin de cette leçon, vous saurez transformer une règle métier énoncée en français en une méthode
dont la signature suffit à comprendre le contrat, avec les entrées invalides traitées d'abord et un
chemin nominal qui tient en quelques lignes lisibles.

## Prérequis

- Avoir lu `reference-types-001` et savoir choisir un type numérique adapté à un montant.
- Savoir déclarer une variable, écrire un `if` et appeler une méthode statique.

## Intuition

Une branche `if` n'est pas un détail d'implémentation : c'est une règle métier écrite en C#. Quand la
règle est claire dans la tête et confuse dans le code, c'est presque toujours que le calcul, la
validation et l'affichage sont mélangés dans la même méthode.

Le réflexe utile consiste à traiter les cas impossibles au tout début, puis à laisser le chemin
normal descendre sans imbrication. On appelle cela une clause de garde.

## Explication

Une méthode prévisible se reconnaît à trois propriétés observables.

**Sa signature dit ce qu'elle fait.** `decimal ComputeShippingFee(decimal orderTotal, bool isExpress)`
annonce deux entrées et une sortie. Un lecteur sait immédiatement qu'il obtiendra un montant et que
rien d'autre ne sera modifié. À l'inverse, `void Process(Order o)` n'annonce rien : il faut lire le
corps pour savoir ce qui change.

**Elle refuse d'abord, calcule ensuite.** Les clauses de garde placent les entrées invalides en haut
de la méthode et sortent immédiatement. Le bénéfice n'est pas esthétique : chaque garde supprime un
niveau d'imbrication du chemin nominal. Une méthode qui commence par trois gardes et continue à plat
se lit du haut vers le bas ; la même méthode écrite avec trois `if` imbriqués oblige à garder trois
conditions en mémoire pour comprendre la dernière ligne.

.NET fournit les gardes courantes, ce qui évite de les réécrire et normalise les messages :
`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`,
`ArgumentOutOfRangeException.ThrowIfNegative`.

**Elle n'a qu'une responsabilité.** Lire une entrée au clavier, décider d'un tarif et afficher le
résultat sont trois responsabilités. Tant qu'elles cohabitent, la règle de tarification ne peut pas
être testée sans console. Une fois séparée, elle devient une fonction de valeurs vers valeur :
appelable depuis un test, depuis une API, depuis un lot nocturne.

Le point de bascule est facile à repérer. Posez la question : *puis-je vérifier cette règle en
appelant une méthode et en comparant le résultat attendu ?* Si la réponse exige une saisie ou une
lecture d'écran, la décision est encore prisonnière de son entrée-sortie.

Sur le choix entre `if` et `switch` : `switch` sur une expression exprime bien une correspondance
entre un cas discret et une valeur, et le compilateur signale les cas non couverts d'une énumération.
`if` reste préférable dès que la condition combine plusieurs variables ou porte sur un intervalle.

Enfin, une méthode prévisible ne modifie pas ses entrées. Le lecteur qui voit
`ComputeShippingFee(total, true)` s'attend à ce que `total` soit intact après l'appel. Toute mutation
silencieuse d'un paramètre est une surprise, et les surprises finissent en tickets.

## Exemple commenté

La règle métier est : *les commandes d'au moins 50 EUR sont livrées gratuitement en standard ;
l'express coûte 9,90 EUR quel que soit le montant ; en dessous de 50 EUR, le standard coûte 4,90 EUR.
Un total négatif est impossible.*

```csharp
public static decimal ComputeShippingFee(decimal orderTotal, bool isExpress)
{
    // Garde : un total négatif n'est pas une valeur métier, c'est un appelant fautif.
    ArgumentOutOfRangeException.ThrowIfNegative(orderTotal);

    // Le cas express est indépendant du montant : le traiter tôt supprime une imbrication.
    if (isExpress)
    {
        return 9.90m;
    }

    // Le chemin nominal tient sur une ligne, sans condition retenue en mémoire.
    return orderTotal >= 50m ? 0m : 4.90m;
}
```

La signature suffit à écrire les tests : `(0, false)`, `(49.99, false)`, `(50, false)`, `(50, true)`
et `(-1, false)`. Aucune console n'est nécessaire, et le seuil de 50 EUR n'apparaît qu'à un seul
endroit.

## Contre-exemple et erreur fréquente

La même règle, écrite en mélangeant les responsabilités :

```csharp
public static void Process()
{
    Console.Write("Total : ");
    decimal total = decimal.Parse(Console.ReadLine()!);   // Plante sur une saisie vide.
    Console.Write("Express ? (o/n) ");
    if (Console.ReadLine() == "o")
    {
        Console.WriteLine("Frais : 9,90");
    }
    else
    {
        if (total >= 50)
        {
            Console.WriteLine("Frais : 0");
        }
        else
        {
            Console.WriteLine("Frais : 4,90");
        }
    }
}
```

Trois défauts, dans l'ordre de gravité. La règle n'est pas testable : il faut simuler une console.
Le montant négatif n'est jamais refusé, donc `-100` obtient 4,90 EUR sans que personne ne le
remarque. Et le seuil de 50 est enfoui dans une imbrication, à côté d'un texte d'affichage : le jour
où il passe à 60, la modification se fait à l'aveugle.

La correction n'est pas de rajouter un `if` : c'est d'extraire la décision dans une méthode qui prend
des valeurs et retourne une valeur, puis de laisser `Process` ne faire que de l'entrée-sortie.

## Vérification de compréhension

Reformulez la règle de tarification en nommant : les entrées, la sortie, la valeur refusée et le
seuil. Si l'un des quatre manque, relisez l'explication avant de coder.

:::quiz
id=csharp-control-methods-001-check
question=Pourquoi placer les clauses de garde au début d'une méthode plutôt qu'imbriquer les conditions ?
option=Parce que le compilateur C# refuse plus de deux niveaux d'imbrication
option=Parce que chaque garde retire un niveau d'imbrication au chemin nominal, qui reste lisible de haut en bas
option=Parce que les gardes rendent la méthode plus rapide à l'exécution
correct=1
success=Correct : la garde traite le cas impossible et sort, ce qui laisse le chemin normal à plat et lisible sans retenir de condition en mémoire.
retry=Relisez le passage sur les clauses de garde : le bénéfice porte sur la lisibilité du chemin nominal, pas sur la performance ni sur une limite du langage.
:::

## Exercice guidé

Ouvrez `csharp-shipping-decision-001` dans `/practice`, puis procédez dans cet ordre.

1. Écrivez la signature avant le corps, et vérifiez qu'elle annonce entrées et sortie.
2. Listez les cas : nominal, seuil exact, juste sous le seuil, entrée refusée.
3. Écrivez les gardes, puis le chemin nominal sans imbrication.
4. Prédisez le résultat de chaque cas **avant** d'exécuter, puis comparez.

Toute prédiction fausse est plus instructive que le code : notez laquelle et pourquoi.

## Exercice autonome

Une règle de remise : *au-delà de 10 articles identiques, 5 % de remise ; au-delà de 50, 12 % ; la
remise ne s'applique jamais à un article déjà soldé.* Écrivez la méthode.

Avant de coder, écrivez vos hypothèses sur : la quantité nulle, la quantité négative, le seuil exact
de 10 et de 50, et le cumul éventuel avec une autre remise. Justifiez le type de retour.

## Débogage

Un ticket indique : « Certaines commandes express de plus de 50 EUR sont facturées 0 EUR de port. »

1. **Symptôme** : notez le montant, le mode et le résultat obtenu contre le résultat attendu.
2. **Hypothèse** : l'ordre des conditions place probablement le test du seuil avant celui du mode.
3. **Preuve** : posez un point d'arrêt sur la première condition évaluée et observez la valeur des
   deux paramètres, sans les modifier.
4. **Prévention** : ajoutez le cas `(50, true)` à la suite de tests — il aurait échoué avant la
   correction.

## Entretien

Question posée à voix haute : *comment décidez-vous qu'une méthode fait trop de choses ?*

Une réponse solide ne cite pas un nombre de lignes. Elle donne un critère observable — par exemple
« je ne peux pas la tester sans simuler une entrée-sortie » ou « son nom exige un *et* » — puis un
exemple vécu de séparation et ce que cette séparation a rendu possible.

## Résumé

- La signature est le premier élément de documentation ; écrivez-la avant le corps.
- Les gardes traitent l'impossible en haut et laissent le chemin nominal à plat.
- Une règle testable est une fonction de valeurs vers valeur, sans entrée-sortie.
- Un seuil métier n'apparaît qu'à un seul endroit du code.

## Cartes de révision

Question : quel signal indique qu'une décision métier est encore prisonnière de son entrée-sortie ?
Réponse attendue : on ne peut pas la vérifier en appelant une méthode et en comparant une valeur.

Question : que garantit une méthode qui ne modifie pas ses paramètres ? Réponse attendue : l'appelant
peut réutiliser ses variables après l'appel sans relire l'implémentation.

## Test de maîtrise

Sans relire, écrivez une méthode qui calcule des frais de retour : gratuits sous 14 jours, 5,90 EUR
entre 15 et 30 jours, refusés au-delà. Donnez la signature, deux gardes, trois cas de test dont un
sur un seuil exact, et expliquez pourquoi vous avez choisi une exception plutôt qu'une valeur de
retour pour le refus.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
