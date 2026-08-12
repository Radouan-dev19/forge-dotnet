# Explication

Tester les frontières de la famille plutôt qu'un seul statut nominal.

Une famille de statuts est un intervalle, et un intervalle a deux frontières. Tester le seul statut nominal valide une implémentation qui se tromperait sur les deux bornes — et l'erreur la plus fréquente porte précisément sur la borne haute, où la famille suivante commence immédiatement.

Quatre valeurs suffisent : les deux bornes, et les deux valeurs qui les encadrent à l'extérieur. C'est le triplet de frontière appliqué deux fois, et c'est le jeu minimal qui prouve que l'intervalle est le bon. La décision est en temps constant.
