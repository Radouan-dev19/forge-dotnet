# Explication

Copier l'entrée puis pousser le maximum vers la fin à chaque passage.

L'invariant rend l'algorithme lisible : après le passage numéro k, les k plus grandes valeurs occupent définitivement la fin du tableau. C'est pourquoi la limite du passage suivant peut décroître, et pourquoi comparer au-delà serait du travail inutile.

La copie n'est pas un détail : trier sur place changerait le tableau de l'appelant, effet qu'un contrat de fonction ne doit produire que s'il l'annonce. Le coût reste quadratique en comparaisons — c'est un algorithme qu'on écrit pour le comprendre, pas pour trier en production, où l'implémentation hybride de la plateforme fera mieux.
