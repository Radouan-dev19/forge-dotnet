# Mapper une erreur publique

Implémentez Submission.ErrorStatus avec la signature fournie. Mettre les erreurs connues sur liste blanche et rabattre les détails internes vers 500.

La correspondance reste déterministe et hors ligne, sans lire aucun message d'exception. Écrivez avant le code : une catégorie connue, la même écrite dans une autre casse, et une catégorie inconnue. Nommez ce qu'un statut trop précis divulguerait d'un défaut interne.

Exemple : entrée `["validation"]`, sortie `400`.
