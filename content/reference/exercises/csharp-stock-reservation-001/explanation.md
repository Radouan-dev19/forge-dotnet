# Explication

Préserver l'invariant de stock positif et accepter exactement la quantité disponible.

La frontière est l'égalité : demander exactement le stock disponible est acceptable, et la refuser laisse un article invendable en rayon. C'est la seule frontière du problème, et c'est celle qu'un jeu d'essai construit sur des valeurs rondes ne touche jamais.

Une quantité demandée négative n'est pas une petite erreur : acceptée, elle reviendrait à augmenter le stock, ce qui transforme une garde manquante en faille métier. Le contrat retourne faux plutôt que de lever, ce qui rend la fonction utilisable directement dans une condition. La décision est en temps constant.
