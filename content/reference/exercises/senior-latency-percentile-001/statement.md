# Calculer un percentile de latence au rang le plus proche

Implémentez `Submission.Percentile` avec la signature fournie. La latence moyenne est le mensonge le
plus poli de l'observabilité : un service peut servir sa moyenne en dix millisecondes pendant qu'un
client sur cent attend deux secondes. Les **percentiles** racontent la distribution — le p50 dit le
client médian, le p99 dit le centième le plus mal servi — et votre fonction les calcule par la
méthode du rang le plus proche.

## Le calcul

La fonction reçoit les latences mesurées, en millisecondes, dans un ordre quelconque, et le
percentile demandé (de 1 à 100) :

1. trier une **copie** croissante des mesures — le tableau de l'appelant reste intact ;
2. viser le rang `⌈percentile × effectif ÷ 100⌉`, indexé depuis un ;
3. rendre la valeur à ce rang, telle quelle.

```text
Percentile([120, 80, 200, 150, 90], 50)  →  120
Percentile([120, 80, 200, 150, 90], 99)  →  200
Percentile([10, 20, 30, 40], 25)         →  10
```

La méthode du rang a deux vertus que les variantes interpolées n'ont pas : elle rend une latence
**réellement vécue** par une requête — jamais une moyenne de deux voisines que personne n'a subie —
et son plafond garantit la promesse du percentile : au moins la part demandée des mesures est
inférieure ou égale à la valeur rendue.

## Les refus

`ArgumentException` pour un tableau vide — un percentile sans mesure n'existe pas — ou une latence
négative. `ArgumentOutOfRangeException` pour un percentile hors de un à cent.

## Avant d'écrire

Prédisez le p90 puis le p100 de dix mesures dont neuf valent une milliseconde et une en vaut cent.
Dites ce que ce contraste enseigne sur les valeurs aberrantes : à partir de quel percentile
deviennent-elles visibles, et qu'est-ce que cela implique pour le choix du percentile d'un objectif
de service ?
