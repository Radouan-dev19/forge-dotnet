# Chiffrer la fenêtre d'attente d'un travail relancé avec recul exponentiel

Implémentez `Submission.RetryWindowSeconds` avec la signature fournie. Votre chaîne d'intégration
relance les travaux qui échouent sur des causes passagères — réseau, dépôt de paquets, machine
saturée — avec un **recul exponentiel** : chaque attente double la précédente, jusqu'à un plafond.
Votre fonction chiffre le temps total d'attente d'une campagne de relances, celui qui occupe un
exécuteur sans rien produire.

## La politique de relance

- la première tentative part immédiatement ;
- après chaque échec sauf le dernier, le travail attend avant de repartir ;
- la première attente vaut le délai de base ; chaque suivante double la précédente ;
- aucune attente ne dépasse le plafond : une fois atteint, il écrête toutes les suivantes.

Rendez la somme des attentes, en secondes. Une seule tentative n'attend jamais : zéro.

```text
RetryWindowSeconds(5, 2, 60)   →  30      (attentes : 2, 4, 8, 16)
RetryWindowSeconds(4, 10, 15)  →  40      (attentes : 10, 15, 15)
RetryWindowSeconds(1, 30, 60)  →  0
```

## Les bornes opérationnelles

Le contrat refuse ce qu'aucune exploitation saine ne configure, avec `ArgumentOutOfRangeException` :
moins d'une tentative ou plus de cent — au-delà, la relance ne masque plus un incident passager, elle
masque une panne ; un délai de base inférieur à une seconde ; un plafond inférieur au délai de base,
qui rendrait la première attente déjà hors contrat ; un plafond au-delà de 86 400 secondes — un
travail qui dort plus d'une journée n'est pas en attente, il est abandonné.

Ces bornes ont une vertu cachée : elles garantissent que la somme tient dans un entier de trente-deux
bits. Sans elles, votre calcul devrait se défendre contre son propre débordement.

## Avant d'écrire

Prédisez la fenêtre de dix tentatives avec une seconde de base et un plafond de huit, puis dites à
partir de quelle tentative le plafond fige les attentes. Comparez la fenêtre à ce qu'elle serait sans
plafond, et dites ce que ce rapport justifie en réunion d'exploitation.
