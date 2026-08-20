# Calculer un échéancier de relances à jitter déterministe

Implémentez `Submission.RetrySchedule` avec la signature fournie. Le recul exponentiel nu a un défaut
célèbre : tous les clients qui échouent au même instant relancent aux mêmes instants, et le serveur
convalescent reçoit des vagues synchronisées — le troupeau tonnant. Le **jitter** casse cette
synchronisation ; votre fonction calcule l'échéancier complet, de façon déterministe pour rester
testable.

## Le calcul

La fonction reçoit le nombre de tentatives, le délai de base, le plafond en millisecondes et une
graine. Elle rend les attentes entre tentatives — il y en a une de moins que de tentatives :

1. la fenêtre de la première attente vaut le délai de base ; chaque fenêtre suivante double,
   écrêtée au plafond ;
2. l'attente de rang k vaut la **moitié entière de sa fenêtre**, plus un décalage déterministe :
   le produit de la graine par k, réduit modulo la moitié plus un.

C'est la politique dite du jitter égal : chaque attente vit dans la moitié haute de sa fenêtre —
jamais en dessous de la moitié, jamais au-delà de la fenêtre — et deux clients aux graines
différentes se désynchronisent.

```text
RetrySchedule(4, 100, 10000, 7)   →  [57, 114, 221]
RetrySchedule(5, 800, 2000, 0)    →  [400, 800, 1000, 1000]
RetrySchedule(1, 500, 1000, 42)   →  []
```

## Les bornes du contrat

`ArgumentOutOfRangeException` pour moins d'une tentative ou plus de dix — au-delà, la relance masque
une panne —, un délai de base inférieur à une milliseconde, un plafond inférieur au délai de base ou
au-delà de 60 000 millisecondes, ou une graine négative. Le produit graine fois rang se calcule en
entier large : une grande graine légitime ne doit pas déborder.

## Avant d'écrire

Prédisez l'échéancier de deux clients aux graines 0 et 1 sur les mêmes bornes, et dites à partir de
quel rang ils divergent. Puis expliquez pourquoi le plancher à la moitié de la fenêtre importe
autant que le plafond : que ferait une attente tirée entre zéro et la fenêtre entière, dans le pire
tirage ?
