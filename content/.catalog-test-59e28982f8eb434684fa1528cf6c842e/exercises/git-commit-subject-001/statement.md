# Valider un sujet de commit

Implémentez Submission.IsCommitSubjectValid avec la signature fournie. Exiger un sujet non vide, borné à 72 caractères et sans point final.

La validation reste déterministe et hors ligne : aucun dépôt n'est interrogé. Écrivez avant le code : un sujet ordinaire, un sujet exactement à la limite, un sujet dépassant d'un caractère, un sujet terminé par un point, et une chaîne de blancs. Nommez ce qu'un sujet trop long devient dans une liste de commits.

Exemple : entrée `["Ajoute les tests API"]`, sortie `true`.
