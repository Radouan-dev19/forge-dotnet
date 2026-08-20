# Détecter un conflit non résolu

Implémentez Submission.HasConflictMarkers avec la signature fournie. Détecter chacun des marqueurs Git avant compilation ou fusion.

La détection reste déterministe et hors ligne : aucun dépôt n'est interrogé. Écrivez avant le code : un texte contenant chacun des trois marqueurs, un texte propre, et un texte vide. Nommez ce qu'un marqueur oublié fait à la branche principale une fois commis.

Exemple : entrée `["\u003C\u003C\u003C\u003C\u003C\u003C\u003C HEAD\nvalue"]`, sortie `true`.
