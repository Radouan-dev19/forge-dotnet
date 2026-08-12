# Explication

Conserver les champs vides ; ce micro-exercice n'annonce pas gérer tout le format CSV.

Deux nombres se ressemblent et ne sont pas égaux : une ligne de n champs contient n moins un séparateur. Compter les virgules donne donc systématiquement un résultat trop petit, et une ligne sans virgule vaut un champ, pas zéro. Seule la chaîne vide vaut zéro.

Retirer les segments vides est l'erreur la plus discrète : elle ne change rien sur une ligne bien remplie et fausse le compte dès qu'un champ est absent, c'est-à-dire précisément le cas que l'on cherche à détecter. Le périmètre est délibérément restreint, sans guillemets ni séparateur échappé : un exercice qui annonce moins que ce qu'il fait vaut mieux qu'un qui prétend davantage.
