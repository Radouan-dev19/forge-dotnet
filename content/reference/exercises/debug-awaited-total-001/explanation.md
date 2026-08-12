# Explication

L'exercice isole l'agrégation des résultats obtenus ; aucun travail lancé ne doit être oublié.

La faute que cet exercice prépare à voir se situe en amont : agréger des résultats avant que tous les travaux lancés soient terminés produit un total silencieusement incomplet. Aucune exception ne le signale, et le défaut ne se manifeste qu'en charge, quand un travail met plus de temps que les autres.

La partie isolée ici est plus simple, et ses deux pièges sont classiques : la collection absente est une faute d'appelant, la collection vide vaut zéro, et l'addition vérifiée traite le dépassement que le parcours ne voit pas. Le parcours est linéaire et l'espace se limite à l'accumulateur.
