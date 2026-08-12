# Explication

Valider les trois valeurs et comparer la fenêtre totale au budget.

La fenêtre avant déclaration d'échec est le produit de l'intervalle par le nombre d'essais, pas leur somme : c'est le temps total pendant lequel un service peut ne pas répondre sans être déclaré en panne. Une fenêtre trop courte transforme un démarrage lent en fausse panne, et les services dépendants ne démarrent jamais.

L'égalité au budget est acceptée : un budget est un plafond. Les trois valeurs se valident avant tout calcul, une valeur non strictement positive ne décrivant aucune fenêtre. Le contrôle de dépassement traite le produit de deux grandes valeurs, qui sortirait silencieusement de la plage entière. La décision est en temps constant.
