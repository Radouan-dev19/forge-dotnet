# Base de code existante : la méthode en quatre temps

## Objectif observable

À la fin de cette leçon, vous saurez aborder un code que vous n'avez pas écrit, sans documentation, en reconstituant sa règle par la méthode en quatre temps — symptôme, hypothèse, preuve, prévention — et en la figeant par des tests de caractérisation avant d'y toucher.

## Prérequis

- Avoir suivi `senior-code-review-001` : lire du code d'autrui et en juger le comportement.
- Connaître la notion de test qui vérifie une sortie attendue sur une entrée donnée.

## Intuition

Tout le parcours part d'un starter vide : l'apprenant écrit son propre code. Le métier réel, lui, consiste surtout à reprendre le code des autres, souvent ancien, souvent sans documentation. La compétence n'est plus d'écrire, mais de comprendre avant de modifier. On ne suppose pas l'intention de l'auteur : on la déduit du comportement observé, et on la fige avant d'y toucher.

## Explication

**Le symptôme d'abord, jamais la cause.** Reprendre un code hérité commence par un fait observable : sur telle entrée, on obtient telle sortie, alors qu'on attendait telle autre. Nommer le symptôme sans deviner la cause évite de partir sur une fausse piste. C'est la même discipline que les laboratoires de débogage, appliquée cette fois à du code que l'apprenant découvre plutôt qu'à un défaut planté dans son propre travail.

**L'hypothèse ensuite, formulée avant de coder.** À partir du symptôme, on formule une hypothèse précise sur la règle du code : « le mot `void` annule la dernière entrée, il ne remet pas le solde à zéro ». Une hypothèse se prouve ou se réfute ; une intuition floue ne se teste pas.

**La preuve par des cas.** On confronte l'hypothèse à des entrées représentatives, y compris les cas limites, et on observe la sortie. Ces observations deviennent des tests de caractérisation : ils figent le comportement actuel, quel qu'il soit, avant toute modification. Le point crucial est là : on écrit ces tests sur le comportement observé, pas sur le comportement souhaité, car le code tourne déjà en production et un contresens produirait un résultat faux qui ressemble à un résultat juste.

**La prévention enfin.** Une fois la règle prouvée et figée, on peut modifier en sécurité : si un test de caractérisation casse, on a changé un comportement, volontairement ou non. La prévention consiste aussi à documenter la règle reconstituée, pour que le prochain qui reprend le code ne reparte pas de zéro.

**Pourquoi l'ordre compte.** Sauter le symptôme mène à corriger un problème mal posé. Sauter la preuve mène à modifier sur une supposition. Sauter les tests de caractérisation mène à casser en production un comportement que personne n'avait compris. Les quatre temps se suivent parce que chacun protège le suivant.

## Exemple commenté

Le noyau décidable de cette leçon reconstitue la règle d'un grand livre hérité :

```csharp
// Règle reconstituée : void annule l'effet de la DERNIÈRE entrée appliquée, il ne remet pas à zéro.
// L'effet signé est empilé pour qu'un void annule un débit dans le bon sens.
public static decimal LegacyBalance(string ledger)
{
    decimal balance = 0m;
    var appliedDeltas = new Stack<decimal>();
    foreach (string rawEntry in ledger.Split(';'))
    {
        string entry = rawEntry.Trim();
        if (entry.Length == 0) { continue; }
        if (entry == "void")
        {
            if (appliedDeltas.Count > 0) { balance -= appliedDeltas.Pop(); }
            continue;
        }
        // ... credit et debit poussent leur effet signé sur la pile
    }
    return balance;
}
```

L'hypothèse sur `void` a été prouvée sur `credit:10;debit:5;void`, qui doit rendre dix et non zéro.

## Contre-exemple et erreur fréquente

Le développeur pressé qui suppose l'intention :

```csharp
// FAUTIF : on suppose que void remet le solde à zéro, sans le prouver.
if (entry == "void") { balance = 0m; }
```

Le symptôme est un solde faux dès qu'un `void` suit plusieurs entrées : sur `credit:10;debit:5;void`, ce code rend zéro alors que la règle héritée rend dix. La correction remplace la supposition par une hypothèse prouvée sur des cas, avant toute modification.

## Vérification de compréhension

Avant le quiz, répondez à voix haute : pourquoi écrit-on des tests de caractérisation sur le comportement observé, et non sur le comportement souhaité ?

:::quiz
id=senior-legacy-001-check
question=Quelle est la première chose à faire avant de modifier un comportement de code hérité que l'on vient de comprendre ?
option=Le réécrire proprement selon les conventions actuelles
option=Écrire des tests de caractérisation qui figent le comportement observé, pour détecter tout changement involontaire
option=Supprimer le code ambigu et le remplacer par ce qui semble correct
correct=1
success=Exact : figer le comportement observé par des tests de caractérisation permet de modifier en sécurité et de détecter tout changement, voulu ou non.
retry=Repensez à ce qui se passe si l'on modifie d'abord et que le comportement d'origine était en réalité correct en production.
:::

## Exercice guidé

Ouvrez l'exercice `senior-legacy-trace-001` dans `/practice`, puis procédez ainsi.

1. Notez le symptôme : sur quelle entrée le résultat surprend, et ce que vous attendiez.
2. Formulez une hypothèse précise sur ce que fait `void`.
3. Prouvez-la sur un cas où crédit et débit se suivent, puis sur deux `void` consécutifs.
4. Écrivez la règle reconstituée avant d'implémenter, comme prévention pour le suivant.

## Exercice autonome

Prenez une fonction d'un projet que vous connaissez mal. Sans lire son intention supposée, déduisez sa règle de trois exécutions, écrivez trois tests de caractérisation, puis proposez une modification et vérifiez qu'aucun test ne casse involontairement.

## Débogage

Un ticket indique : « Le calcul des remises hérité donne parfois un montant négatif, sur des paniers que personne n'arrive à reproduire de tête. »

1. **Symptôme** : montant négatif sur certaines entrées, non reproductible de mémoire.
2. **Hypothèse** : une entrée particulière — un cumul, un ordre — déclenche un chemin non prévu.
3. **Preuve** : rejouer les paniers réels sur la fonction isolée et repérer l'entrée qui bascule le signe.
4. **Prévention** : figer par un test de caractérisation le panier fautif, puis corriger sans casser les autres.

## Entretien

Question posée à voix haute : *on vous confie un calcul hérité sans documentation, avec une opération au nom trompeur ; comment procédez-vous avant de le modifier ?*

Une réponse solide refuse de supposer l'intention et la déduit du comportement, écrit des tests de caractérisation avant tout changement, et identifie l'état à suivre pour reproduire l'opération ambiguë. Elle rappelle que le code tourne déjà en production, donc qu'un contresens produit un résultat faux d'apparence juste.

## Résumé

- Le métier consiste surtout à reprendre du code d'autrui, pas à partir d'un starter vide.
- La méthode en quatre temps : symptôme, hypothèse, preuve, prévention.
- On déduit la règle du comportement observé, on ne suppose jamais l'intention.
- Les tests de caractérisation figent le comportement actuel avant toute modification.

## Cartes de révision

Question : quelles sont les quatre étapes de la reprise d'un code hérité ? Réponse attendue : symptôme observé, hypothèse précise, preuve sur des cas, prévention par tests de caractérisation et documentation.

Question : pourquoi ne modifie-t-on pas un code hérité avant de l'avoir figé par des tests ? Réponse attendue : parce qu'il tourne déjà en production et qu'un contresens produit un résultat faux d'apparence juste, qu'aucun test ne détecterait sans caractérisation préalable.

## Test de maîtrise

Sans relire, appliquez la méthode en quatre temps à une fonction héritée de votre choix : nommez le symptôme, l'hypothèse, la preuve que vous chercheriez, et la prévention que vous mettriez en place avant de modifier.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
