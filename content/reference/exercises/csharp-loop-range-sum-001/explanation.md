# Explication

Un accumulateur initialisé à zéro reçoit chaque valeur exactement une fois. La condition `current <= end` rend la borne finale inclusive. Le retour anticipé pour une plage inversée documente le cas vide sans entrer dans la boucle.

L’invariant est : avant chaque incrément, `total` contient la somme de `start` jusqu’à la valeur précédente. La méthode parcourt `n` entiers et n’alloue aucune collection.
