# Explication

Évaluer le privilège explicite puis l'identité exacte de la ressource.

Deux niveaux, et l'omission du second est la faille la plus répandue des interfaces web : vérifier qu'un appelant a le droit d'agir sur ce type de ressource, sans vérifier qu'il a le droit d'agir sur celle-ci. Elle ne produit aucune erreur — le code fonctionne, pour tout le monde — et il suffit de changer un identifiant dans une adresse.

Le cas des identités absentes mérite sa branche : deux valeurs vides sont égales, et une comparaison naïve accorderait donc l'accès. La comparaison est ordinale, sans équivalence culturelle ni tolérance de casse, parce qu'un identifiant est une valeur technique et non un texte lisible. Le coût est linéaire dans la longueur des identifiants.
