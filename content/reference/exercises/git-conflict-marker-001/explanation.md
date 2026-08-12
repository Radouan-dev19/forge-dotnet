# Explication

Détecter chacun des trois marqueurs de conflit avant compilation ou fusion.

Les trois marqueurs se cherchent séparément parce qu'une résolution partielle en laisse souvent un seul : le séparateur oublié au milieu d'un bloc réécrit, ou la fermeture restée en fin de fichier. Ne chercher que l'ouverture laisse donc passer les cas les plus courants.

Le contrôle a une valeur pratique simple : il coûte une seconde avant de commettre, là où un marqueur commis casse la construction pour toute l'équipe. Compter sur le compilateur revient à découvrir le problème après l'avoir publié. Le parcours est linéaire dans la taille du fichier.
