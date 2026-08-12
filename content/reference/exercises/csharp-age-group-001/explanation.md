# Explication

Tester chaque frontière et conserver un état invalide distinct.

Quatre issues, trois frontières : zéro, dix-huit et soixante-cinq. Chaque comparaison est strictement inférieure, donc l'âge de la frontière appartient à la tranche supérieure — dix-huit ans est adulte, pas mineur. C'est exactement là que se loge l'erreur de un.

L'ordre des conditions porte du sens : chaque branche suppose que les précédentes ont échoué, ce qui permet à la dernière de ne rien tester. Inverser cet ordre rend une branche inatteignable sans qu'aucun avertissement ne le signale. L'état invalide reste une quatrième valeur, distincte de la première tranche : un âge négatif n'est pas un mineur.
