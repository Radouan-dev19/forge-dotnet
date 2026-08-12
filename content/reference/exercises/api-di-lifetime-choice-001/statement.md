# Choisir une durée de vie DI

Implémentez Submission.LifetimeFor avec la signature fournie. L’état de requête impose scoped ; un service partagé doit être explicitement sans état.

La décision reste déterministe et hors ligne, sans conteneur d'injection. Écrivez avant le code : les quatre combinaisons des deux indicateurs, en indiquant laquelle prime lorsqu'ils sont vrais tous les deux. Nommez ce qu'un mauvais choix produit en charge.

Exemple : entrée `[true,false]`, sortie `"scoped"`.
