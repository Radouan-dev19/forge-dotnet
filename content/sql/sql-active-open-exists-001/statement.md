# Filtrer avec EXISTS

Écrivez une requête bornée qui retourne `CustomerId` et `Name` pour les clients actifs ayant au moins une commande ouverte, ordonnés par identifiant.

Un client doit apparaître une seule fois, quel que soit son nombre de commandes ouvertes. N'utilisez ni objet serveur, ni référence inter-base, ni donnée externe.
