# Explication

`decimal` représente exactement les valeurs décimales usuelles d’un montant. La multiplication par `100m` doit précéder la conversion en entier, sinon la partie décimale disparaît.

Le mode `MidpointRounding.AwayFromZero` rend le traitement des demi-centimes explicite et testable. La validation du signe précède le calcul : une donnée métier invalide ne devient pas silencieusement une valeur valide.

La méthode effectue toujours le même nombre d’opérations, donc sa complexité est constante.
