# Explication

Faire primer la création, puis distinguer ressource trouvée et ressource absente.

La création est un résultat, pas un cas particulier de lecture : elle a son propre statut et elle prime. Évaluer la présence en premier ferait retourner un statut de lecture pour une ressource qui vient d'être créée, ou pire, un statut d'absence — puisqu'elle n'existait pas avant l'appel.

Le reste est la distinction la plus banale d'une interface web, et la plus souvent mal traitée : une ressource absente n'est pas un succès avec un corps vide. Le statut porte l'information, et un client bien écrit se fie à lui avant de regarder le corps. La décision est en temps constant.
