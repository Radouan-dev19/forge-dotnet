# Explication

Ignorer zéro et les négatifs sans modifier le tableau reçu.

Zéro n'est ni positif ni négatif : la comparaison est strictement supérieure, et l'inclure ne changerait rien au total mais changerait la règle. Un tableau vide donne légitimement zéro — c'est une somme vide, pas une erreur — alors qu'un tableau absent est une faute d'appelant qui doit lever.

L'addition vérifiée n'est pas une précaution décorative : sans elle, une suite de grandes valeurs positives produit un total négatif, résultat faux qu'aucun cas nominal ne révèle. Le parcours est linéaire et seul l'accumulateur occupe l'espace.
