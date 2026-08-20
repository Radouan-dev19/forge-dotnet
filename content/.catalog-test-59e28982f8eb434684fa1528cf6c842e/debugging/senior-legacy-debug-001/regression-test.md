# Non-regression

Ajoutez, et gardez, un cas de caracterisation sur une **derniere page partielle** : un total qui
n'est pas un multiple de la taille de page doit rendre une page de plus que la division exacte.

- 10 elements par pages de 3 rendent 4 pages.
- 101 elements par pages de 10 rendent 11 pages.
- 1 element par pages de 3 rend 1 page.

Gardez aussi les cas aux bornes qui doivent rester stables : un total de 0 rend 0 page, un multiple
exact rend le quotient exact, une taille de page inferieure a 1 leve une exception d'argument. Ces cas
figent le comportement attendu et signalent toute regression future de l'arrondi.
