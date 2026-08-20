# Ordonner la compensation d'une saga

Implementez `Submission.CompensationOrder` avec la signature fournie. La fonction recoit la liste des
etapes d'une saga qui ont **reussi** avant qu'une etape suivante n'echoue, et rend la suite des
actions compensatoires a executer pour revenir a un etat coherent.

## Le format

`completedSteps` est une suite de noms d'etapes separes par des points-virgules, dans l'ordre ou
elles ont ete appliquees. Les blancs autour d'un nom ne comptent pas et les segments vides sont
ignores.

```text
completedSteps = "reserve;charge;ship"
```

## La regle

Une saga ne dispose pas d'une transaction unique : chaque etape a deja produit un effet visible dans
un autre service. Pour revenir en arriere, on execute une **action compensatoire** par etape reussie,
dans l'ordre **strictement inverse** de leur application : la derniere etape posee est la premiere
annulee, car les etapes precedentes peuvent encore etre necessaires a son annulation.

Chaque etape `s` devient l'action `undo-s`. Les actions sont jointes par une virgule-point ... non :
par un point-virgule, sans espace. Une saga sans aucune etape reussie rend une **chaine vide**. Un
corps absent leve `ArgumentNullException`.

## Avant d'ecrire

Predisez la sortie pour trois cas : une saga de trois etapes, une saga d'une seule etape, et une saga
vide. Nommez ce qui casse si l'on compense dans l'ordre direct plutot qu'inverse.
