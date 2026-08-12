# Lire un jeton de concurrence pédagogique

Écrivez une requête bornée qui retourne `OrderId` et `DataVersion` pour les commandes ouvertes, ordonnées par identifiant.

La colonne de version est lue telle quelle : elle sert de jeton de concurrence à une mise à jour ultérieure. N'utilisez ni objet serveur, ni référence inter-base, ni donnée externe.
