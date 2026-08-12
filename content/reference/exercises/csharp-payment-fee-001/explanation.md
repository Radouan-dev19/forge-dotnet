# Explication

La branche représente deux implémentations d'une même politique de frais.

Deux moyens de paiement, deux taux, une seule mécanique : c'est la forme la plus simple d'une politique substituable. Le jour où un troisième moyen apparaît, seule la table des taux change, pas le calcul — et c'est exactement ce qu'une interface ferait à plus grande échelle.

Le reste est de l'arithmétique monétaire : type décimal pour représenter exactement les valeurs de la base dix, multiplication avant l'unique arrondi, et mode d'arrondi explicite plutôt que celui de la plateforme, qui tranche vers le pair le plus proche. La décision est en temps constant.
