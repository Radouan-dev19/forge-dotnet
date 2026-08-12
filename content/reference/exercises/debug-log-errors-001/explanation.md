# Explication

Chercher le marqueur exact et avancer après chaque occurrence.

La boucle de recherche a deux points de rupture. Oublier d'avancer la position produit une boucle infinie, symptôme qu'on ne diagnostique pas en lisant le code mais en constatant qu'un service ne répond plus. Avancer d'un seul caractère compte les recouvrements, ce qui gonfle le résultat sans raison visible.

La recherche est ordinale et sensible à la casse : le marqueur d'un niveau de journal est une convention exacte, et une comparaison tolérante compterait un mot du message. Compter par ligne serait une troisième erreur : une même ligne peut porter plusieurs occurrences. Le parcours est linéaire dans la longueur du journal.
