# Explication

Trois tarifs, deux critères : la grille de livraison est minuscule, et l'énoncé demande pourtant
d'écrire *l'ordre des décisions* avant de coder. C'est que la grille contient un piège d'ordre,
et qu'une règle métier mal ordonnée rend des montants faux avec une assurance parfaite.

Le piège : un client express dont le panier dépasse quatre-vingts euros. La règle de gratuité
s'applique-t-elle ? Non — le contrat dit « un envoi express coûte *toujours* 9,90 » — et ce mot
« toujours » se traduit en code par la *priorité* : le test du mode express vient avant le test
de gratuité, et retourne sans regarder le total. La version qui teste la gratuité d'abord
offrirait l'express aux gros paniers, une erreur commerciale qu'aucun cas nominal ne montre —
il faut le cas croisé, express *et* gros total, que les cas cachés posent précisément.
L'enseignement dépasse la livraison : quand deux règles peuvent s'appliquer à la même entrée,
leur ordre est une décision métier à écrire, pas un accident d'implémentation. Les grilles
tarifaires, les régimes de remise et les politiques d'accès regorgent de ces recouvrements.

La structure de la solution rend cette priorité lisible : validation, puis sortie express, puis
la règle standard en dernière expression. Chaque `return` évacue un régime entier, et le code
restant travaille sur un domaine réduit — le style gardes-puis-décision déjà rencontré dans les
classements, appliqué cette fois à des règles qui se recouvrent.

La borne de gratuité est *incluse* — « au moins 80 » s'écrit `>=` — et le cas posé exactement à
quatre-vingts euros départage les écritures : gratuit, pas 4,90. Une borne d'argent mal incluse
est le litige client le plus courant des sites marchands ; le test à la frontière coûte une
ligne et vaut le prix d'un ticket de support.

Le montant négatif est refusé avant toute décision — un panier négatif ne décrit rien, et le
valider d'abord garantit qu'aucun régime ne s'applique à une donnée absurde. Les montants sont
en `decimal` de bout en bout, et les tarifs sont des littéraux décimaux exacts — pas de calcul,
pas d'arrondi nécessaire ici, la grille rend des constantes.

Le coût est constant. La transposition est le questionnaire à dérouler devant toute grille :
quelles règles se recouvrent, laquelle gagne, la borne est-elle incluse, que fait-on du hors
domaine ? Quatre réponses écrites avant le code — c'est exactement ce que l'énoncé exige, et
c'est la différence entre une grille tarifaire et une collection de `if` qui a l'air de
marcher.
