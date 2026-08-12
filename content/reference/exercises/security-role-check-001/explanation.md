# Explication

Comparer chaque rôle complet sans recherche partielle ni valeur implicite.

La recherche de sous-chaîne est le défaut central : un rôle attendu se retrouve à l'intérieur d'un rôle plus long, et un compte obtient un droit qu'il ne détient pas. Découper puis comparer des segments entiers est la seule forme correcte, et elle coûte à peine plus cher.

Les blancs de bordure autour des segments viennent de la sérialisation de la liste et sont invisibles à la lecture : les retirer évite un refus incompréhensible. Enfin, l'absence de rôle déclaré est un cas normal — un compte peut n'en porter aucun — donc elle se traite par un refus, pas par une exception qui transformerait un contrôle d'accès en erreur interne. Le coût est linéaire dans la longueur de la liste.
