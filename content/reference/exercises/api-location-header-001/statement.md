# Construire une localisation de ressource

Implémentez Submission.OrderLocation avec la signature fournie. Refuser un identifiant non publié puis construire une route relative stable.

La composition reste déterministe et hors ligne, sans lire d'hôte dans l'environnement. Écrivez avant le code : un identifiant ordinaire, la plus petite valeur acceptée, et le refus d'un identifiant nul. Nommez ce qu'une adresse absolue casserait au premier changement d'environnement.

Exemple : entrée `[42]`, sortie `"/orders/42"`.
