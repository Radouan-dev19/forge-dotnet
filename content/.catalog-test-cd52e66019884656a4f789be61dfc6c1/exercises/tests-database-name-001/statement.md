# Reconnaître une base de test isolée

Implémentez Submission.IsIsolatedDatabase avec la signature fournie. Exiger un préfixe réservé et un suffixe suffisamment unique, avec comparaison ordinale.

La vérification reste déterministe et hors ligne, sans ouvrir aucune connexion. Écrivez avant le code : un nom conforme, un nom au préfixe absent, un nom dont le suffixe est trop court d'un caractère, et un nom vide. Nommez ce que le préfixe réservé rend sûr au moment du nettoyage.

Exemple : entrée `["forge-test-123456789"]`, sortie `true`.
