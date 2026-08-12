# Explication

Ne retenir qu'une clé publique autorisée et choisir la valeur par défaut sinon.

Un nom de colonne ne peut pas être un paramètre de requête : il est concaténé, quoi qu'on fasse. La paramétrisation protège les valeurs, pas les identifiants, et c'est la raison pour laquelle une liste fermée définie dans le code est ici le seul contrôle qui protège réellement.

Une clé inconnue retombe sur le tri par défaut plutôt que de lever : le client obtient un résultat ordonné au lieu d'une erreur, et le contrat reste utilisable depuis une interface qui construit ses paramètres. La liste fermée a un second bénéfice : elle rend les tris possibles énumérables dans le document de contrat. Le coût est linéaire dans la longueur de la clé.
