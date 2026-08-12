# Explication

Un changement d'autorisation est toujours à risque élevé ; le volume affine les autres cas.

La nature du changement prime sur sa taille, et c'est contre-intuitif : trois lignes modifiant une condition d'autorisation méritent plus d'attention que trois cents lignes de tests. Évaluer le volume en premier ferait classer le premier cas en risque faible, c'est-à-dire exactement le diff qu'une revue rapide laisserait passer.

Les deux seuils ne servent qu'aux changements sans enjeu d'autorisation, et ils se testent séparément dans les deux sens. Un volume négatif ne décrit aucun diff : le refuser évite de produire un classement qui n'a pas de sens. La décision est en temps constant.
