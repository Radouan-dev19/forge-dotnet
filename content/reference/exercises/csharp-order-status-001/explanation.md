# Explication

Rendre le cas inconnu explicite au lieu d'inventer un état valide.

Une correspondance sur des codes venus de l'extérieur est toujours partielle : un code nouveau apparaîtra, et la question n'est pas s'il faut le prévoir mais ce qu'on en fait. Le rattacher au dernier état connu produit une réponse plausible et fausse, la pire des deux. Une étiquette inconnue explicite laisse l'appelant décider.

C'est aussi la raison pour laquelle un état publié gagne à voyager sous forme de texte plutôt que d'entier : un entier publié devient un contrat que l'on ne peut plus réordonner, et le décalage d'une unité entre deux versions ne se voit nulle part. La décision est en temps constant.
