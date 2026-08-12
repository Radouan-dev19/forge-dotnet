# Explication

Ignorer les frames système et conserver la première frame applicative dans l'ordre.

Une trace se lit de haut en bas : la première ligne est le point de levée, souvent au fond d'une bibliothèque, et elle n'apprend rien. La première frame appartenant au code de l'application est le point d'entrée du diagnostic — c'est là que la responsabilité commence.

Parcourir en sens inverse donnerait la frame la plus haute de la pile applicative, c'est-à-dire le point d'appel initial : information utile, mais différente, et beaucoup moins précise pour localiser la cause. L'absence de frame applicative n'est pas une erreur : elle signifie que le défaut est entièrement hors du code de l'application. Le parcours est linéaire dans la taille de la trace.
