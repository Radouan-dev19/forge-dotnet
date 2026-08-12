# Explication

Dans cette représentation pédagogique, zéro signifie explicitement nœud absent.

La convention est le sujet : un tableau ne porte pas d'information de présence par lui-même, il faut la coder. Ici la valeur nulle marque l'absence, ce qui a un coût — un nœud portant réellement la valeur nulle devient indistinguable d'une absence. Une représentation qui vise davantage emploierait un type optionnel plutôt qu'une valeur sentinelle.

S'arrêter à la première absence serait une seconde hypothèse, celle d'un remplissage sans trou, que la représentation ne garantit pas. Le parcours est complet, linéaire, et n'occupe qu'un compteur.
