# Autoriser propriétaire ou administrateur

Implémentez Submission.CanEdit avec la signature fournie. Évaluer le privilège explicite puis l’identité exacte de la ressource.

La décision reste déterministe et hors ligne, sans consulter aucun annuaire. Écrivez avant le code : le propriétaire légitime, un tiers refusé, l'administrateur qui passe outre, et deux identités absentes. Nommez ce qu'un contrôle limité à l'action laisserait faire en changeant un identifiant d'adresse.

Exemple : entrée `["u1","u1",false]`, sortie `true`.
