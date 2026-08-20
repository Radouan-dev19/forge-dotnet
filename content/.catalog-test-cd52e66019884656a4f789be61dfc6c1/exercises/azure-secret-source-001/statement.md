# Choisir une source de valeur sensible

Implémentez Submission.SensitiveValueSource avec la signature fournie. Une valeur non sensible reste en configuration ; une valeur sensible utilise lʼidentité gérée ou un magasin local hors Git.

La décision reste déterministe et hors ligne, et n'emploie aucune valeur ressemblant à un secret réel. Écrivez avant le code : les quatre combinaisons des deux indicateurs, en vérifiant lequel tranche en premier. Nommez ce qu'une identité attestée par la plateforme supprime entièrement.

Exemple : entrée `[false,true]`, sortie `"configuration"`.
