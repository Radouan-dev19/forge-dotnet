# Composer une clé de configuration

Implémentez Submission.ConfigKey avec la signature fournie. Valider les deux segments et conserver le séparateur hiérarchique standard.

La composition reste déterministe et hors ligne, sans lire aucune configuration réelle. Écrivez avant le code : une paire valide, une paire dont un segment porte des blancs de bordure, et le refus d'un segment vide. Nommez ce qu'une clé mal formée produit au démarrage.

Exemple : entrée `["Authentication","ApiKey"]`, sortie `"Authentication:ApiKey"`.
