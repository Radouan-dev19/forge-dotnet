# Explication

Une remise fidélité de dix pour cent : le calcul tient sur une ligne, et la ligne contient
pourtant une forme — net égale total fois un moins le taux — dont le choix face à son
alternative est la première chose à savoir défendre.

L'alternative calcule la remise puis soustrait : montant de remise égale total fois taux, net
égale total moins remise. Les deux formes sont algébriquement identiques *tant que personne
n'arrondit au milieu*. Dès qu'un arrondi intermédiaire s'invite — arrondir la remise au centime
avant de soustraire, ce que font beaucoup de systèmes pour l'afficher — les deux chemins
divergent d'un centime sur certains montants, et le total facturé ne correspond plus au net
calculé ailleurs. La solution retient la forme directe avec un *unique* arrondi final : le net
est défini comme une quantité, pas comme le résidu d'une autre quantité arrondie. Quand le
métier exigera d'afficher la remise elle-même, il faudra choisir — remise dérivée du net, ou
net dérivé de la remise — et documenter lequel des deux est la référence ; l'exercice installe
le réflexe de voir qu'il y a un choix.

La structure sépare la *politique* du *calcul* : le statut choisit un taux — dix pour cent ou
zéro — et la formule du net ne varie pas. Le client ordinaire passe par la même multiplication
avec un taux nul, plutôt que par un court-circuit qui rendrait le total brut : uniformité du
chemin de sortie, même arrondi pour tous, aucun risque qu'une branche dérive vers sa propre
convention. C'est le même patron que les frais de paiement voisins, décliné sur une remise — et
la répétition est voulue : ce découpage politique-mécanique est celui qui s'étendra en table de
taux ou en interface quand les statuts se multiplieront.

Les décisions d'argent restent non négociables : `decimal` de bout en bout, littéraux exacts
`0.10m` et `1m`, arrondi au centime avec la règle commerciale nommée — `AwayFromZero`, pas le
défaut bancaire — et le cas caché posé sur un demi-centime de net qui départage les règles. Le
total négatif est refusé avant toute politique : la remise d'un montant absurde n'existe pas.
Zéro, lui, est licite et rend zéro — remisé ou non.

Le coût est constant. La transposition tient en trois questions à poser devant toute remise,
taxe ou majoration : quelle est la quantité de référence — le net ou l'écart ? Où est l'unique
arrondi ? Et la politique est-elle séparée du calcul qu'elle paramètre ? Trois réponses écrites,
et le code de tarification cesse de produire des centimes fantômes.
