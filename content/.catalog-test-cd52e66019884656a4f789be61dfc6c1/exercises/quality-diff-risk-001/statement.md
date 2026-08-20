# Estimer le risque d’un diff

Implémentez Submission.DiffRisk avec la signature fournie. Un changement d’autorisation est toujours haut risque ; le volume affine les autres cas.

Le classement reste déterministe et hors ligne, sans lire aucun dépôt. Écrivez avant le code : un petit diff touchant l'autorisation, un gros diff qui ne la touche pas, les deux seuils exacts, et un volume négatif. Nommez ce qu'un classement par volume seul laisserait passer en revue.

Exemple : entrée `[20,false]`, sortie `"low"`.
