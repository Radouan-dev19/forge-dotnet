# Explication

Recevoir la date observée au lieu de lire l'horloge système dans la règle.

Une règle qui lit l'heure courante n'est pas testable : son résultat dépend du moment d'exécution, donc le test passe aujourd'hui et échoue un jour donné. Fournir la date observée en paramètre déplace la lecture de l'horloge vers la couche appelante, où elle appartient, et rend la règle reproductible.

La comparaison est stricte : le jour de l'échéance, l'abonnement est encore valide. C'est la seule frontière du problème, et c'est celle qu'un test construit sur « hier » et « demain » ne touche jamais. La décision est en temps constant.
