# Chiffrer le budget d'erreur restant d'une fenêtre de service

Implémentez `Submission.RemainingErrorBudget` avec la signature fournie. Un objectif de niveau de
service ne promet jamais la perfection : il promet, par exemple, 99,9 pour cent de réussite sur une
fenêtre donnée. Le complément — le dixième de pour cent restant — est le **budget d'erreur** : la
quantité d'échecs que l'équipe peut dépenser en incidents, en déploiements risqués, en maintenance.
Votre fonction dit combien il en reste.

## Le calcul

La fonction reçoit le volume total de requêtes de la fenêtre, le nombre d'échecs déjà consommés, et
l'objectif en pourcentage. Le budget alloué est la part tolérée appliquée au volume, **arrondie vers
le bas** ; le restant est l'alloué moins le consommé — et il peut être négatif : un budget dépassé se
chiffre, il ne se masque pas.

```text
RemainingErrorBudget(10000, 5, 99.9)   →  5      (alloué : 10)
RemainingErrorBudget(10000, 12, 99.9)  →  -2     (dépassement)
RemainingErrorBudget(500, 0, 99.0)     →  5
```

## Pourquoi le plancher et pourquoi le décimal

Sur 999 requêtes à trois neufs, la part tolérée vaut 0,999 requête : arrondir au plus proche
offrirait un échec entier que l'objectif ne concède pas. Le plancher est le seul arrondi qui ne
contourne jamais la promesse. Et le calcul passe par le type décimal : les pourcentages d'objectif —
99,9, 99,95, 99,99 — n'ont pas de représentation exacte en flottant binaire, et l'erreur de
représentation finit par déplacer le plancher d'une unité.

## Les refus

`ArgumentException` pour un volume ou des échecs négatifs, ou des échecs supérieurs au volume — la
fenêtre décrite n'existe pas. `ArgumentOutOfRangeException` pour un objectif nul, négatif ou au-delà
de cent. Cent tout rond est accepté : c'est un budget nul, exigeant mais cohérent.

## Avant d'écrire

Prédisez le restant pour une fenêtre de 999 requêtes sans aucun échec à trois neufs, et dites
pourquoi ce résultat — surprenant au premier regard — est exactement ce que l'objectif signifie.
