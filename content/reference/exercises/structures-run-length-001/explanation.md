# Explication

Incrémenter seulement lors d'un changement par rapport au caractère précédent.

Un texte non vide contient au moins un groupe : c'est la valeur de départ, et l'oublier décale tout le résultat de un. Ensuite, un nouveau groupe commence exactement là où le caractère diffère du précédent, ce qui explique que le parcours démarre au deuxième caractère — le premier n'a pas de précédent.

La chaîne vide est le seul cas à zéro groupe, et il doit être traité avant la valeur de départ. Le parcours est linéaire et deux variables suffisent : il n'est jamais nécessaire de mémoriser les groupes eux-mêmes pour les compter.
