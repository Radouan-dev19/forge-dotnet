# Explication

Valider les deux invariants puis multiplier avant l'unique arrondi.

L'ordre des opérations est la règle métier. Arrondir avant de multiplier reporte l'écart sur chaque unité, et une ligne de cent articles accumule cent fois cet écart. Un seul arrondi, à la fin, borne l'erreur à un centime pour la ligne entière.

Deux choix de type et de mode complètent la règle. Le décimal représente exactement les valeurs de la base dix, ce que la virgule flottante binaire ne fait pas — un centime y devient une valeur voisine. Et le mode d'arrondi est explicite : le mode par défaut de la plateforme tranche vers le pair le plus proche, ce qui n'est pas ce qu'annonce une facture. La décision est en temps constant.
