# Classer un signal corrélé

Implémentez Submission.IncidentSignal avec la signature fournie. Valider les mesures, traiter les erreurs en priorité, puis comparer la latence p95 au budget de 750 ms.

Le classement reste déterministe et hors ligne : aucune ressource distante n'est interrogée. Écrivez avant le code : une erreur observée, une latence au budget exact, une latence dépassant le budget d'une milliseconde, un état sain, et une mesure négative. Nommez ce qu'une alerte sur la latence seule laisserait passer.

Exemple : entrée `[2,300]`, sortie `"errors"`.
