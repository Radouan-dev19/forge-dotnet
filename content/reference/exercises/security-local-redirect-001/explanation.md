# Explication

Accepter un chemin racine local mais refuser les formes réseau et les adresses absolues.

Le contrôle s'écrit en n'acceptant que le connu, jamais en interdisant le connu-mauvais. Une liste de domaines interdits est contournable par construction ; une règle qui n'accepte qu'un chemin local ne l'est pas.

Deux formes ressemblent à un chemin local sans en être un. Le double séparateur désigne un autre hôte tout en commençant par un séparateur ; et la variante au séparateur inversé est normalisée par certains clients en une adresse réseau. Les deux doivent être refusées explicitement, sans quoi le contrôle laisse passer exactement ce qu'il devait bloquer. Le coût est linéaire dans la longueur de la destination.
