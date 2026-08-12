# Explication

Compter les deux bornes et retourner zéro pour un intervalle inversé.

Un intervalle inclusif compte ses deux extrémités : du lundi au lundi, il y a un jour, pas zéro. Le plus un n'est donc pas un ajustement mais la définition, et l'omettre produit une erreur d'exactement un jour à chaque appel — assez petite pour survivre à une relecture, assez grande pour fausser une facturation.

L'intervalle inversé retourne zéro plutôt qu'un nombre négatif : un compte de jours négatif ne signifie rien pour l'appelant et se propagerait silencieusement. Travailler sur des numéros de jour plutôt que sur une durée évite en prime toute question de fuseau et de changement d'heure. La décision est en temps constant.
