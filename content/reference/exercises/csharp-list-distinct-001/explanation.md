# Explication

La liste résultat conserve l’ordre du parcours. Un ensemble d’entiers mémorise les valeurs rencontrées ; sa méthode `Add` retourne `true` uniquement lors de la première insertion. Cette valeur décide directement si l’élément rejoint le résultat.

L’entrée reste inchangée car seules les nouvelles collections sont modifiées. Le coût est linéaire en moyenne, avec un espace proportionnel au nombre de valeurs distinctes.
