# Explication

Choisir la politique puis arrondir au point métier annoncé.

La fonction retourne le net, pas la remise : multiplier par le complément du taux le dit en une opération, là où soustraire une remise calculée séparément introduit un second arrondi. Deux arrondis sur le même calcul produisent un écart d'un centime sur certaines valeurs, écart qui ne se voit qu'en rapprochant une facture d'un relevé.

Le reste tient aux règles d'arithmétique monétaire déjà rencontrées : type décimal pour l'exactitude en base dix, un seul arrondi en fin de calcul, et un mode explicite plutôt que celui de la plateforme. La décision est en temps constant.
