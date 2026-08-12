# Explication

Trier une copie puis traiter séparément longueurs paires et impaires.

La médiane a deux définitions selon la parité, et c'est la seule difficulté du problème : l'élément central pour une longueur impaire, la moyenne des deux éléments centraux pour une longueur paire. Traiter les deux cas de la même façon donne un résultat juste une fois sur deux.

La moyenne se calcule dans un type décimal : diviser deux entiers tronquerait la demie, et le résultat serait faux exactement quand les deux valeurs centrales diffèrent d'un nombre impair. Le tri s'applique à une copie, pour la même raison que dans le reste de ce module : l'ordre observé est une donnée du diagnostic. Le tri domine le temps.
