# Refuser une redirection externe

Implémentez Submission.IsLocalRedirect avec la signature fournie. Accepter un chemin racine local mais refuser les formes réseau et les URL absolues.

Le contrôle reste déterministe et hors ligne, sans consulter aucune liste de domaines. Écrivez avant le code : un chemin local accepté, une adresse absolue, la forme à double séparateur et celle au séparateur inversé. Nommez ce qu'une redirection ouverte permet de faire d'un utilisateur authentifié.

Exemple : entrée `["/orders/1"]`, sortie `true`.
