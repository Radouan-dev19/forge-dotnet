# Explication

Refuser les termes vides puis employer une comparaison ordinale insensible à la casse.

Un terme vide est le piège de tout filtre : il correspond à tout, donc il ne filtre rien, et le point d'entrée retourne la collection entière alors que l'appelant croyait chercher. Le refuser explicitement est plus sûr que d'espérer que personne ne l'envoie.

La comparaison ordinale insensible à la casse est le choix par défaut pour un filtre technique : elle ne dépend pas de la culture de la machine, donc le même appel donne le même résultat partout. Passer par deux conversions de casse produirait deux allocations et introduirait les mêmes dépendances culturelles qu'on cherchait à éviter. Le coût est proportionnel au produit des longueurs au pire.
