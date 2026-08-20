# Vérifier un socle de durcissement

Implémentez Submission.IsHardened avec la signature fournie. Exiger simultanément utilisateur non-root, lecture seule et no-new-privileges.

La décision reste déterministe et hors ligne : aucun conteneur n'est exécuté. Écrivez avant le code : la configuration complète, puis les trois cas où un seul réglage manque. Nommez ce que l'absence d'interdiction d'élévation permet malgré une identité non privilégiée.

Exemple : entrée `[true,true,true]`, sortie `true`.
