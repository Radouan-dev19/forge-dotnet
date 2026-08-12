# Explication

Retourner la première position ou le code d'absence après un parcours complet.

Le code d'absence ne peut pas être zéro : zéro est un indice parfaitement valide, et le confondre avec « non trouvé » rend la fonction inutilisable dès que la cible occupe la première case. Une valeur hors du domaine des indices est le seul choix sûr, et moins un est la convention établie.

Le parcours s'arrête à la première correspondance : c'est ce que « première position » signifie, et poursuivre donnerait la dernière. Le coût est linéaire, prix à payer sur une entrée non triée — un tableau trié permettrait une recherche logarithmique, mais imposerait le maintien de l'ordre.
