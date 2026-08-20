# Caractériser un calcul de frais hérité avant de le remanier

Implémentez `Submission.LegacyShippingFee` avec la signature fournie. Vous héritez d'un calcul de
frais de livraison écrit il y a huit ans, sans test, que personne n'ose toucher. Avant de le remanier,
vous écrivez sa **caractérisation** : une implémentation de référence qui reproduit exactement ce que
la production fait — pas ce que la documentation promet.

## Le relevé d'observation

Voici ce que le système en production répond, mesuré sur des commandes réelles :

- un panier de zéro article ou moins est refusé avec `ArgumentException` ;
- un sous-total négatif est refusé avec `ArgumentException` ;
- quand le sous-total dépasse **strictement** 100, la livraison est gratuite : `0.00` ;
- à 100 tout rond, les frais s'appliquent — la règle affichée promet « gratuit dès cent », le code
  fait autre chose, et les clients le vivent depuis huit ans ;
- sinon, les frais valent `4.90`, plus `0.50` par article au-delà du cinquième.

```text
LegacyShippingFee(60.00, 3)    →  4.90
LegacyShippingFee(100.00, 2)   →  4.90
LegacyShippingFee(100.01, 12)  →  0.00
```

## Ce que caractériser veut dire

Le but n'est pas d'écrire le bon calcul : c'est d'écrire le calcul **actuel**, bizarrerie comprise.
Cette implémentation de référence deviendra le filet du remaniement : chaque écart entre elle et le
code remanié sera un changement de comportement, voulu ou non, détecté avant la production. Corriger
la comparaison stricte du seuil en la rendant large serait précisément l'erreur : le filet signalerait
alors le comportement réel comme une régression, et la vraie correction — si l'équipe la décide un
jour — partirait d'un référentiel faux.

Les montants observés portent deux décimales, comme la facturation les affiche : `5.40`, jamais `5.4`.

## Avant d'écrire

Prédisez les frais pour un panier de six articles à 75, pour un panier de cinq articles à 99,99, et
pour un article unique à 250. Dites, pour chacun, quelle ligne du relevé d'observation décide.
