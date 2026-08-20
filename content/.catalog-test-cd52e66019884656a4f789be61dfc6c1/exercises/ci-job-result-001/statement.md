# Calculer le résultat d’un job CI

Implémentez Submission.JobResult avec la signature fournie. Le job réussit seulement si construction et tests réussissent tous les deux.

La décision reste déterministe et hors ligne : aucune chaîne d'intégration n'est interrogée. Écrivez avant le code : les quatre combinaisons des deux signaux, en vérifiant que seule leur conjonction produit le succès. Nommez ce qu'un travail annoncé réussi avec des tests rouges fait croire à l'équipe.

Exemple : entrée `[true,true]`, sortie `"success"`.
