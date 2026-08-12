# Explication

Ajouter l'entrant, retirer le sortant, puis publier chaque fenêtre complète.

La somme glissante ne recalcule rien : chaque avancée coûte une addition et une soustraction, quelle que soit la taille de la fenêtre. C'est la différence entre un parcours linéaire et un parcours quadratique, et elle se voit dès que la fenêtre dépasse quelques éléments.

Deux indices méritent d'être posés avant d'écrire : le nombre de fenêtres vaut la longueur moins la taille, plus un ; et la première somme n'est publiable qu'à partir de l'indice égal à la taille diminuée de un. Une taille invalide retourne un résultat vide plutôt qu'une exception, ce qui rend la fonction utilisable sans garde préalable. L'espace correspond au résultat produit.
