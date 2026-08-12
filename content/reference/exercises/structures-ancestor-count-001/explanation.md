# Explication

Valider chaque parent et borner le parcours pour détecter un cycle.

Une représentation par tableau de parents ne garantit rien : rien n'empêche une entrée de pointer hors de l'intervalle, ni deux nœuds de se désigner mutuellement. Un parcours écrit en supposant un arbre bien formé boucle alors indéfiniment, et le symptôme est un service qui ne répond plus, pas une exception.

La garde transforme une donnée corrompue en refus explicite : au-delà d'autant de sauts qu'il y a de nœuds, un cycle est certain, puisqu'un chemin sans cycle ne peut pas être plus long. La racine ne compte pas parmi ses propres ancêtres, et un nœud sans parent en a zéro. Le parcours est proportionnel à la hauteur et n'occupe qu'un compteur.
