# Choisir un statut HTTP

Implémentez Submission.StatusFor avec la signature fournie. Faire primer la création, puis distinguer ressource trouvée et absente.

La décision reste déterministe et hors ligne, sans consulter aucun stockage. Écrivez avant le code : les quatre combinaisons des deux indicateurs, et laquelle prime lorsqu'ils se contredisent. Nommez ce qu'un statut de succès sur une ressource absente ferait croire au client.

Exemple : entrée `[true,false]`, sortie `200`.
