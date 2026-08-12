# Explication

Compter les occurrences non chevauchantes et respecter la casse du journal.

Avancer d'un seul caractère après chaque trouvaille compte les recouvrements : sur un identifiant fait de caractères répétés, le résultat explose sans qu'aucune occurrence supplémentaire n'existe réellement. Avancer de la longueur de l'identifiant est ce qui définit « non chevauchant ».

La recherche est ordinale : un identifiant de corrélation est une valeur technique, et une comparaison tolérante confondrait deux traces distinctes. Le journal se traite comme un texte continu plutôt que ligne à ligne, sans quoi deux occurrences sur la même ligne n'en feraient qu'une. Le parcours est linéaire dans la longueur du journal.
