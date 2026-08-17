# Compter les couches reconstruites

Implémentez `Submission.RebuiltLayers` avec la signature fournie.

Une image se construit par couches empilées : chaque étape part de l'état laissé par la précédente.
Quand une étape change, son résultat change — et tout ce qui était bâti dessus doit être refait.

## Le contrat

```csharp
public static int RebuiltLayers(int totalSteps, int changedStep)
```

`totalSteps` est le nombre d'étapes du fichier de construction. `changedStep` est le rang de la
première étape modifiée, **compté à partir de un**.

Rendez le nombre d'étapes reconstruites : l'étape modifiée **et toutes celles qui la suivent**.

```text
10 étapes, la 8e change  ->  3 étapes reconstruites (la 8e, la 9e, la 10e)
10 étapes, la 1re change ->  10
```

## Les refus

`ArgumentOutOfRangeException` si `totalSteps` est inférieur à un, ou si `changedStep` ne désigne pas
une étape existante — un rang hors bornes rendrait un compte négatif, c'est-à-dire une réponse qui ne
veut rien dire.

## Ce que le calcul enseigne

Le compte ne dépend pas de ce que fait l'étape. Modifier une ligne de commentaire dans une étape
précoce coûte autant qu'en réécrire la commande : le cache raisonne sur l'empreinte de l'instruction
et du contexte, pas sur son sens.

C'est de là que vient la règle d'écriture d'un fichier de construction : **ce qui change rarement se
place en haut, ce qui change à chaque commit se place en bas**. Copier les sources avant de restaurer
les dépendances annule le cache de la restauration à chaque modification, et la construction repart
de loin — plusieurs minutes, à chaque fois.

## Avant d'écrire

Prédisez quatre cas : une modification en dernière étape, en première étape, au milieu, et un rang
qui n'existe pas. Nommez ce que coûte une copie de sources placée trop haut.
