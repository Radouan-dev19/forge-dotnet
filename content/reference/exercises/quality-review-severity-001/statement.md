# Classer une remarque de revue

Implémentez Submission.ReviewSeverity avec la signature fournie. Prioriser un risque de sécurité, puis un défaut de correction, puis la suggestion.

Le classement reste déterministe et hors ligne, sans lire aucun diff. Écrivez avant le code : les quatre combinaisons des deux indicateurs, en vérifiant laquelle prime lorsqu'ils sont vrais tous les deux. Nommez ce que des remarques non hiérarchisées font perdre à l'auteur.

Exemple : entrée `[true,false]`, sortie `"blocker"`.
