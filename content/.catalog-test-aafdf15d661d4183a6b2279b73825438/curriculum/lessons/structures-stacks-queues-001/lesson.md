# Piles et files par intention

## Objectif observable

À la fin de cette leçon, vous saurez choisir entre une pile et une file en partant de la question
métier plutôt que de l'API, et vous saurez reconnaître les trois familles de problèmes — appariement,
parcours, fenêtre glissante — où l'une des deux structures divise la complexité.

## Prérequis

- Avoir lu `algo-simple-sorts-001` et savoir énoncer un invariant de boucle.
- Savoir instancier une collection générique et parcourir une chaîne caractère par caractère.

## Intuition

Le choix ne porte pas sur la structure de données mais sur **l'ordre de traitement que le métier
impose**. Défaire la dernière action, revenir au dernier appel, fermer la dernière balise ouverte :
c'est une pile. Traiter dans l'ordre d'arrivée, servir le premier client, dépiler un flux
d'événements : c'est une file.

Écrire `Stack` ou `Queue` plutôt que `List` ne change presque rien à la performance sur de petits
volumes. Ce que cela change, c'est que le nom du type documente l'intention, et qu'une opération
interdite devient impossible à écrire.

## Explication

**Le vocabulaire d'abord.** Une pile est LIFO — dernier entré, premier sorti. `Push` empile, `Pop`
dépile et retire, `Peek` regarde sans retirer. Une file est FIFO — premier entré, premier sorti.
`Enqueue` ajoute à la fin, `Dequeue` retire du début, `Peek` regarde le prochain. Dans les deux cas,
ces opérations sont en O(1) amorti.

**Pourquoi pas une `List`.** On peut simuler une pile avec `list.Add` et `list.RemoveAt(Count - 1)` :
c'est correct et efficace. Mais simuler une file avec `list.Add` et `list.RemoveAt(0)` est un piège :
retirer le premier élément d'une liste **décale tous les autres**, donc coûte O(n). Une file
construite ainsi transforme un traitement de n événements en O(n²). `Queue<T>` utilise un tampon
circulaire et n'a pas ce coût.

La seconde raison est contractuelle : une `List` expose l'insertion au milieu et l'accès par indice.
Si votre algorithme dépend de l'ordre LIFO, autoriser `list.Insert(3, x)` c'est laisser un futur
lecteur casser l'invariant sans s'en apercevoir.

**Famille 1 — l'appariement.** Parenthèses, balises, blocs imbriqués, annulation d'actions : dès
qu'un élément doit se refermer dans l'ordre inverse de son ouverture, c'est une pile. Le motif est
toujours le même : empiler à l'ouverture, dépiler et vérifier la correspondance à la fermeture, et
exiger que la pile soit vide à la fin. Cette dernière vérification est celle qu'on oublie : sans
elle, `((` est jugé valide.

**Famille 2 — le parcours.** Un parcours en profondeur utilise une pile — c'est d'ailleurs ce que
fait la récursion, avec la pile d'appels du processeur. Un parcours en largeur utilise une file.
Passer d'une pile à une file dans un parcours de graphe change l'ordre de visite sans changer une
autre ligne : c'est le même code. Ce point revient en `structures-trees-001`.

**Famille 3 — la fenêtre glissante.** Calculer un maximum, une somme ou une moyenne sur une fenêtre
mobile appelle une file : on ajoute l'élément entrant et on retire le sortant, en O(1) par pas au lieu
de recalculer la fenêtre entière. Une variante, la file à double extrémité, permet même de maintenir
le maximum d'une fenêtre en O(1) amorti — c'est l'un des rares cas où la structure change la classe
de complexité, pas seulement la constante.

**Ce que la pile explicite apporte sur la récursion.** Un parcours récursif profond finit par saturer
la pile d'appels — quelques dizaines de milliers de niveaux suffisent. Réécrire le parcours avec une
`Stack<T>` explicite déplace l'état sur le tas, qui est bien plus grand, et supprime le risque de
débordement. La logique est identique ; seul le support de l'état change.

**Le piège du parcours pendant la modification.** Comme pour toute collection, retirer d'une pile ou
d'une file pendant qu'on l'énumère invalide l'énumérateur. Le motif correct consiste à boucler sur
`while (stack.Count > 0)` et à dépiler dans le corps — jamais à faire un `foreach` sur la structure
qu'on modifie.

## Exemple commenté

Vérification d'un parenthésage à trois types de délimiteurs — le cas d'école de l'appariement :

```csharp
public static bool IsBalanced(string text)
{
    ArgumentNullException.ThrowIfNull(text);

    var pending = new Stack<char>();
    foreach (char character in text)
    {
        switch (character)
        {
            case '(' or '[' or '{':
                pending.Push(character);      // On mémorise ce qui devra être refermé.
                break;

            case ')' or ']' or '}':
                // Une fermeture sans ouverture en attente est déjà une erreur.
                if (pending.Count == 0 || Closing(pending.Pop()) != character)
                {
                    return false;
                }

                break;
        }
    }

    // Vérification souvent oubliée : « ((( » n'a produit aucune erreur ci-dessus.
    return pending.Count == 0;

    static char Closing(char opening) => opening switch
    {
        '(' => ')',
        '[' => ']',
        _ => '}',
    };
}
```

Un seul parcours, O(n) en temps, O(n) en espace au pire — le cas où tout est ouvert. Les cas qui
prouvent l'implémentation : chaîne vide (valide), `()`, `([)]` (invalide, croisement), `(((`
(invalide, non fermé) et `)))` (invalide, fermeture orpheline).

Et la file pour une fenêtre glissante, où le gain de complexité est direct :

```csharp
// Somme glissante : chaque pas ajoute l'entrant et retire le sortant.
// O(n) au total, là où recalculer chaque fenêtre coûterait O(n * taille).
public static IReadOnlyList<int> WindowSums(IReadOnlyList<int> values, int size)
{
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
    var window = new Queue<int>(size);
    var sums = new List<int>();
    int running = 0;

    foreach (int value in values)
    {
        window.Enqueue(value);
        running += value;
        if (window.Count > size)
        {
            running -= window.Dequeue();   // O(1) : Queue ne décale rien.
        }

        if (window.Count == size)
        {
            sums.Add(running);
        }
    }

    return sums;
}
```

## Contre-exemple et erreur fréquente

```csharp
public static List<string> ProcessInOrder(List<string> incoming)
{
    var processed = new List<string>();
    while (incoming.Count > 0)
    {
        string next = incoming[0];
        incoming.RemoveAt(0);       // O(n) : tous les éléments suivants sont décalés.
        processed.Add(Transform(next));
    }

    return processed;
}
```

Deux défauts, l'un de performance et l'autre de contrat.

`RemoveAt(0)` décale l'intégralité de la liste à chaque appel. Le traitement de n éléments coûte donc
O(n²). Sur 1 000 événements c'est invisible ; sur 100 000, la méthode passe de quelques millisecondes
à plusieurs secondes. Le remède est `Queue<T>`, dont `Dequeue` est en O(1).

Le second défaut est plus insidieux : la méthode **vide la liste de l'appelant**. Rien dans sa
signature ne l'annonce — elle retourne une nouvelle liste, ce qui suggère au contraire qu'elle ne
touche à rien. Un appelant qui affiche `incoming.Count` après l'appel obtient zéro sans comprendre
pourquoi. Soit la méthode consomme et son nom le dit, soit elle reçoit un `IReadOnlyList` et
construit sa propre file en interne.

## Vérification de compréhension

Pour un système d'annulation d'actions et pour un traitement de messages entrants, dites quelle
structure convient à chacun et quelle propriété métier justifie ce choix.

:::quiz
id=structures-stacks-queues-001-check
question=Quel est le coût réel du traitement de n éléments avec une boucle qui appelle RemoveAt(0) sur une liste à chaque tour ?
option=O(n), puisque chaque élément n'est traité qu'une seule fois
option=O(n au carré), car retirer le premier élément d'une liste décale tous les suivants
option=O(n log n), car la liste se réorganise à chaque suppression
correct=1
success=Correct : RemoveAt(0) est en O(n) sur une liste. Une file utilise un tampon circulaire et retire en temps constant.
retry=Relisez le passage sur les raisons de ne pas simuler une file avec une liste, et ce qui se passe en mémoire lors du retrait du premier élément.
:::

## Exercice guidé

Ouvrez `structures-balanced-parentheses-001` dans `/practice`, puis procédez ainsi.

1. Écrivez les cinq cas de test avant tout code, dont la chaîne vide et le croisement.
2. Implémentez avec une pile, sans jamais indexer la structure.
3. Vérifiez explicitement que la pile est vide à la fin — c'est la vérification la plus oubliée.
4. Comparez vos prédictions aux résultats et notez tout écart.

## Exercice autonome

Écrivez une méthode qui évalue une expression en notation postfixée, par exemple `3 4 + 2 *`.

Décidez avant de coder : la structure retenue et pourquoi, le comportement sur expression vide, sur
opérateur sans assez d'opérandes, et sur expression laissant plusieurs valeurs à la fin. Ce dernier
cas est celui que la plupart des implémentations oublient.

## Débogage

Un ticket indique : « Le traitement de la file d'import ralentit fortement au-delà de dix mille
lignes. »

1. **Symptôme** : la dégradation est superlinéaire, sans erreur.
2. **Hypothèse** : la file est simulée par une liste et le retrait en tête décale tout.
3. **Preuve** : mesurez le temps sur 1 000 puis 10 000 lignes. Un facteur dix en entrée qui produit un
   facteur cent en durée confirme le O(n²).
4. **Prévention** : basculez sur `Queue<T>` et ajoutez un test de charge sur un volume représentatif
   de la production.

## Entretien

Question posée à voix haute : *dans quel cas préférez-vous une pile explicite à la récursion ?*

Une réponse solide cite la profondeur : la pile d'appels est limitée, le tas ne l'est pas dans les
mêmes proportions. Elle mentionne aussi le contrôle qu'on gagne — pouvoir suspendre, reprendre ou
inspecter l'état du parcours — et reconnaît que la récursion reste plus lisible quand la profondeur
est bornée.

## Résumé

- Le choix se déduit de l'ordre de traitement imposé par le métier, pas de l'API.
- Simuler une file avec une liste coûte O(n) par retrait, donc O(n²) au total.
- Appariement, parcours en profondeur, annulation : une pile.
- Ordre d'arrivée, parcours en largeur, fenêtre glissante : une file.
- Après un appariement, la pile doit être vide — c'est la vérification qu'on oublie.

## Cartes de révision

Question : quelle vérification finale manque le plus souvent dans un contrôle de parenthésage ?
Réponse attendue : que la pile soit vide à la fin, sans quoi une ouverture non fermée passe pour
valide.

Question : quel parcours de graphe obtient-on en remplaçant la pile par une file ? Réponse attendue :
on passe du parcours en profondeur au parcours en largeur, sans changer le reste du code.

## Test de maîtrise

Sans relire, écrivez une méthode qui, pour chaque élément d'un tableau, retourne le prochain élément
strictement plus grand situé à sa droite, ou -1. Justifiez la structure choisie, annoncez la
complexité et expliquez pourquoi une approche par double boucle serait quadratique.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
