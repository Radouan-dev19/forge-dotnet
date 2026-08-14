# Explication

Additionner deux montants : la fonction la plus courte du catalogue, sans garde, sans boucle,
sans branche. Elle existe parce que la seule décision qu'elle contient — le type — est celle qui
gouverne tout le reste du domaine monétaire, et qu'il faut l'avoir comprise *avant* les
exercices qui la supposent acquise.

`decimal` additionne en base dix. Dix plus cinq et demi font exactement quinze et demi, et
l'égalité est exacte au sens fort : le résultat est représenté tel quel, comparable au centime
près, affichable sans reformatage correctif. La même addition en `double` — le type binaire —
donnerait une valeur *proche* de quinze et demi : le binaire ne sait pas représenter la plupart
des fractions décimales, chaque littéral est déjà une approximation, et chaque opération
propage l'écart. Sur une addition isolée, l'erreur est invisible ; sur une chaîne de calculs —
totaux de lignes, taxes, remises — les écarts s'accumulent jusqu'au centime qui fait la
différence entre deux rapports comptables. L'énoncé dit « sans conversion binaire » : c'est
l'interdiction de faire transiter le montant par `double` ou `float`, même temporairement, car
un seul aller-retour suffit à perdre l'exactitude.

Pourquoi un exercice entier pour cela ? Parce que le choix du type est *invisible dans le
résultat des cas simples* — tous les tests naïfs passent en `double` aussi — et que c'est
exactement le genre de décision qu'on ne peut pas rattraper après coup : un système qui a
stocké des `double` monétaires ne retrouve jamais les centimes perdus. La signature de la
méthode, imposée en `decimal` des deux côtés, est ici le professeur : elle montre que la
protection se place dans les *types* des frontières — paramètres, colonnes, contrats d'API — et
non dans des corrections d'arrondi dispersées en aval.

La fonction n'arrondit pas, et c'est correct : aucun point métier n'est annoncé ici, et
l'addition de deux décimaux exacts est exacte — arrondir serait une promesse non demandée. Les
exercices suivants du domaine introduisent l'arrondi précisément là où un contrat le demande,
jamais par habitude. Les cas cachés font varier signes et décimales — les montants négatifs
sont licites dans cette fonction générique, un avoir plus une facture se somme — et réfutent la
sortie recopiée de l'exemple.

Le coût est une addition. La transposition est une règle d'architecture qui tient en une
phrase et s'applique à chaque nouveau projet : l'argent entre en `decimal`, circule en
`decimal`, se stocke en décimal — et toute apparition de `double` dans un chemin monétaire est
un défaut à signaler en revue, même si tous les tests passent.
