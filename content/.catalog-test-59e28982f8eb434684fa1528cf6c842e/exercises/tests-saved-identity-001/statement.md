# Vérifier une identité persistée

Implémentez Submission.HasSavedIdentity avec la signature fournie. Une intégration réussie produit un identifiant strictement positif observable.

La vérification reste déterministe et hors ligne, sans ouvrir aucune connexion. Écrivez avant le code : un identifiant attribué, la valeur nulle qui signale l'absence d'attribution, et une valeur négative. Nommez ce qu'une relecture depuis le contexte qui vient d'écrire pourrait retourner sans que rien n'ait atteint la base.

Exemple : entrée `[1]`, sortie `true`.
