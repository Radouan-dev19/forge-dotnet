# Explication

Trois bandes de température, deux frontières : l'exercice est un classement par gardes ordonnées
réduit à l'os, et c'est voulu — toute l'attention se porte sur les deux endroits où l'énoncé
exige des « décisions explicites » : zéro et vingt.

À zéro degré, il ne gèle plus : la garde `celsius < 0` range zéro côté `frais`. À vingt degrés,
il fait chaud : `celsius < 20` échoue pour vingt, qui tombe dans le `return "chaud"` final.
Chacune de ces affectations aurait pu être inverse — un thermomètre qui affiche zéro peut
raisonnablement dire « gel » — et c'est exactement le point : la physique ne tranche pas, le
*contrat* tranche, et le code doit refléter le contrat au caractère près. Le `<` contre `<=`
n'est pas une nuance de style, c'est l'endroit où la spécification devient exécutable. Les cas
cachés posent des valeurs sur les deux frontières exactes, ainsi que leurs voisines immédiates —
moins un, un, dix-neuf, vingt-et-un — pour encadrer chaque bascule des deux côtés.

La structure mérite d'être relue avec les yeux d'un relecteur. Trois sorties, des gardes
ordonnées du plus froid au plus chaud, chaque condition s'appuyant sur l'échec des précédentes :
quand `celsius < 20` s'évalue, on sait déjà que la valeur est positive ou nulle, donc la bande
`frais` est bien l'intervalle de zéro à dix-neuf sans qu'aucun `&&` n'ait à l'écrire. La
dernière bande n'a pas de condition du tout — c'est le reste du domaine — et cette absence est
une propriété de complétude : aucune température n'échappe au classement, aucun ordre de test ne
laisse de trou. Une cascade de conditions indépendantes devrait prouver ces deux propriétés à la
main ; les gardes ordonnées les donnent par construction.

Il n'y a pas de bande d'erreur ici, contrairement au classement d'âges voisin : toute valeur du
type est une température plausible dans ce domaine simplifié, y compris les négatifs profonds.
C'est un contrat plus permissif, et le comparer à celui des âges — où le négatif est un état
`invalid` distinct — montre que la présence ou l'absence d'un cas hors domaine est elle-même
une décision, pas une évidence.

Le coût est constant, deux comparaisons au plus. La transposition est celle de tous les
barèmes à seuils — niveaux d'alerte, classes de consommation, zones tarifaires : écrire les
frontières dans le contrat avec leurs inclusions, les transcrire en gardes ordonnées, et poser
un cas de test *sur* chaque frontière plus un de chaque côté. Le classement est alors prouvé
par ses bords, qui sont les seuls endroits où il peut se tromper.
