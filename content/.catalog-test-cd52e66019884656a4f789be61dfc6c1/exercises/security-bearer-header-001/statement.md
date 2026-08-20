# Valider le schéma Bearer

Implémentez Submission.HasBearerToken avec la signature fournie. Vérifier le schéma et la présence d’une preuve sans jamais retourner sa valeur.

La vérification reste déterministe et hors ligne, et n'emploie aucune valeur ressemblant à une preuve réelle. Écrivez avant le code : un en-tête bien formé, le schéma écrit dans une autre casse, le schéma seul, et un en-tête absent. Nommez ce qu'une preuve journalisée exposerait.

Exemple : entrée `["Bearer fake-token"]`, sortie `true`.
