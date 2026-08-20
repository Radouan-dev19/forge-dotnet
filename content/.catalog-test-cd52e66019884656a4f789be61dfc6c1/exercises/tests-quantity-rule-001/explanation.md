# Explication

Une plage inclusive de un à cent, écrite en un motif : `value is >= 1 and <= 100`. L'exercice
tient dans la consigne — *exprimer directement la plage pour rendre ses quatre frontières
testables* — et dans la question de l'énoncé : que prouvent ces quatre valeurs qu'une valeur
intérieure ne prouve pas ?

La réponse d'abord, car elle justifie tout : cinquante prouve seulement qu'*au moins une*
valeur passe — presque toutes les implémentations plausibles, justes ou décalées, acceptent
cinquante. Les quatre valeurs de frontière — un, cent, zéro, cent un — épinglent chacune un
caractère du code : un qui passe prouve que la borne basse est incluse ; zéro qui échoue prouve
qu'elle est bien à un et pas à zéro ; cent qui passe prouve l'inclusion haute ; cent un qui
échoue prouve la position du plafond. Quatre assertions, quatre caractères de code verrouillés
— `>=` contre `>`, `1` contre `0`, et symétriquement en haut. C'est la méthode des valeurs
limites appliquée à une plage complète : deux frontières, chacune testée des deux côtés.

La forme du code sert cette testabilité, et c'est le second enseignement. Le motif
`is >= 1 and <= 100` énonce la plage *en un seul endroit*, dans l'ordre de lecture
mathématique. Les formes équivalentes éclatées — deux `if` négatifs, une expression avec
négation — disent la même chose en obligeant le lecteur à reconstruire la plage de tête ; et
une plage difficile à lire est une plage dont les tests de frontière se trompent de cible. La
lisibilité de la règle et la testabilité de ses bornes sont le même sujet : on teste bien ce
qu'on lit bien.

Le choix du verdict — un booléen, pas une exception — situe la fonction dans la taxonomie du
catalogue : c'est un prédicat de validation, appelé pour trier, pas une garde de frontière qui
refuse. Les deux formes coexistent dans une API réelle — le prédicat au service de la réponse
d'erreur agrégée, la garde aux points d'entrée internes — et savoir lequel on écrit évite les
exceptions utilisées comme messages.

Les cas cachés sont le plan de test lui-même : les quatre frontières, plus une valeur
intérieure pour le nominal et une lointaine pour l'évidence. Le coût est constant.

La transposition est double. Côté test : toute plage d'un contrat génère mécaniquement ses
quatre cas de frontière, et un plan qui n'en pose que deux — les bornes sans leurs voisines
extérieures — n'a verrouillé que la moitié des caractères. Côté écriture : chaque plage mérite
sa forme directe et localisée — un motif, une constante nommée par borne — parce que la
prochaine personne à modifier le plafond doit pouvoir le faire en touchant un seul nombre, et
que ses tests de frontière doivent casser si elle oublie l'un des deux bouts.
