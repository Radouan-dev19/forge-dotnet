# Ticket — la derniere page de resultats manque a l'appel

## Contexte

Un ecran de liste hérité affiche les elements par pages. Le nombre de pages a parcourir est calcule
par une fonction que personne dans l'equipe n'a ecrite, et qui n'a pas de documentation.

## Symptome observe

Sur un catalogue de 10 elements affiches par pages de 3, l'interface propose **3 pages** de
navigation, alors que 3 pages de 3 ne montrent que 9 elements : le dixieme n'est jamais atteignable.
Le probleme n'apparait que lorsque le nombre total d'elements n'est pas un multiple exact de la taille
de page ; quand il l'est, la navigation semble correcte.

## Attendu

Pour 10 elements par pages de 3, la navigation devrait proposer 4 pages, la derniere ne contenant
qu'un element. Aucun element publie ne doit rester hors d'atteinte de la pagination.

## Ce qui est demande

Reproduire le symptome sur une entree non multiple, situer l'ecart entre le nombre de pages rendu et
le nombre attendu, corriger, puis figer un cas de non-regression sur une derniere page partielle.
