# Explication

Parcourir les caractères évite une liste fragile de ponctuations. Un tampon contient le mot courant. À chaque séparateur, une fonction locale transfère ce mot dans le dictionnaire puis vide le tampon. Le même transfert après la boucle traite le dernier mot.

`char.ToLowerInvariant` fournit une normalisation indépendante de la culture de la machine. Chaque caractère est lu une fois.
