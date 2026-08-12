# Explication

L'index atteint la longueur et progresse de un à chaque appel.

Le cas de base se place à la longueur, pas à la longueur diminuée de un : c'est ce qui rend le tableau vide correct sans branche supplémentaire, et ce qui évite de perdre le dernier élément. Écrire la condition d'arrêt un cran trop tôt est l'erreur la plus fréquente sur une récursion indexée, et elle produit un résultat presque juste.

La progression stricte de l'index garantit la terminaison ; le contrôle de dépassement traite le cas que la récursion ne voit pas. Un point mérite d'être noté : la pile croît avec la longueur du tableau, ce qui rend cette forme élégante mais impropre aux très grandes entrées — une boucle n'aurait pas cette limite.
