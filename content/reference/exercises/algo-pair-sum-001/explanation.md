# Explication

Chercher le complément avant d'ajouter la valeur courante pour exiger deux positions.

L'ordre des deux opérations porte toute la règle. Chercher d'abord garantit que le complément trouvé occupe une position antérieure, donc que la paire est faite de deux éléments distincts. Ajouter d'abord accepterait qu'une valeur égale à la moitié de la cible soit appariée avec elle-même, ce qui n'est pas une paire.

Ce même ordre laisse passer le cas légitime où deux éléments distincts portent la même valeur : le second trouve le premier dans l'ensemble. L'ensemble ramène le coût de quadratique à linéaire en moyenne, au prix d'un espace proportionnel à l'entrée — l'échange le plus courant en algorithmique.
