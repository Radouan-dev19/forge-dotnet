# Vérifier un rôle déclaré

Implémentez Submission.HasRole avec la signature fournie. Comparer chaque rôle complet sans recherche partielle ni valeur implicite.

La vérification reste déterministe et hors ligne, sans consulter aucun annuaire de rôles. Écrivez avant le code : un rôle réellement détenu, un rôle dont le nom est contenu dans un autre, des segments entourés de blancs, et une liste vide. Nommez le droit qu'une recherche partielle accorderait à tort.

Exemple : entrée `["Reader,Operator","Operator"]`, sortie `true`.
