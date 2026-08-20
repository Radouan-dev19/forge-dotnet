# Démasquer les tests couplés par l'ordre d'exécution

Implémentez `Submission.OrderDependentTests` avec la signature fournie. Votre chaîne d'intégration a
rejoué la même suite plusieurs fois en mélangeant l'ordre des tests, et vous recevez le journal de ces
exécutions. Votre fonction en extrait les tests dont le verdict dépend de l'ordre — ceux qui minent la
confiance de toute l'équipe dans la suite.

## Le format du journal

Les exécutions sont séparées par une barre verticale. Dans une exécution, les entrées sont séparées
par des virgules et s'écrivent `Nom=ok` ou `Nom=ko`, dans l'ordre réel de passage.

```text
OrderDependentTests("Alpha=ok,Beta=ko|Beta=ok,Alpha=ok")  →  "Beta"
OrderDependentTests("Cache=ok,Clock=ok|Clock=ok,Cache=ok") →  ""
```

## Ce qu'il faut produire

Un test est **dépendant de l'ordre** quand il a reçu au moins deux verdicts différents à travers les
exécutions. Rendez les noms de ces tests, triés par ordre ordinal, joints par des virgules. Quand
aucun test ne diverge — y compris quand le journal ne contient qu'une seule exécution, qui ne prouve
rien à elle seule — rendez la chaîne vide.

Notez ce que la définition ne dit pas : un test qui échoue **partout** n'est pas dépendant de l'ordre.
Il est cassé, ce qui est un problème plus simple et plus honnête.

## Les refus

`ArgumentException` pour un journal vide ou blanc, une entrée sans verdict lisible, un verdict autre
que `ok` ou `ko`, un nom répété au sein d'une même exécution, ou deux exécutions qui ne couvrent pas
exactement le même ensemble de tests — comparer des campagnes différentes fabriquerait de fausses
divergences.

## Avant d'écrire

Prédisez le résultat pour trois exécutions où un test passe, échoue, puis repasse ; pour un test
absent de la deuxième exécution ; et pour un test qui échoue trois fois de suite. Dites lequel des
trois relève de cet exercice et pourquoi les deux autres n'en relèvent pas.
