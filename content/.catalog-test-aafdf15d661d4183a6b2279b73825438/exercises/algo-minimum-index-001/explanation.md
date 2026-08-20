# Explication

Chercher l'*indice* du minimum n'est pas chercher le minimum, et la différence structure toute la
solution. L'appelant veut savoir *où* se trouve la plus petite valeur — pour la retirer, la
remplacer, ou lire ce qui l'entoure — et la valeur elle-même reste accessible par `values[min]`.
L'accumulateur est donc un indice initialisé à zéro, et la comparaison se fait entre
`values[i]` et `values[min]` : garder deux accumulateurs, la valeur et sa position, serait
correct mais redondant, et la version à indice unique rend impossible leur désynchronisation —
une classe de bug de moins par construction.

Le mot « premier » du contrat se joue dans un seul caractère. La comparaison stricte
`values[i] < values[min]` laisse l'indice en place à égalité : sur un tableau qui contient deux
fois la valeur minimale, c'est la première position qui sort. Écrire `<=` inverserait
silencieusement la promesse — la dernière position sortirait — et aucun test sur des valeurs
toutes distinctes ne le verrait. Les cas cachés placent précisément des doublons du minimum pour
départager les deux écritures, et déplacent le minimum en tête et en queue pour éprouver les
bornes du parcours qui démarre à un.

Le tableau vide relève d'une convention, et elle est ici différente de celle du maximum voisin :
`-1`, l'indice impossible, plutôt qu'une valeur par défaut. La raison est que le domaine de sortie
s'y prête — tout indice valide est positif ou nul, donc `-1` est un « rien » sans ambiguïté, la
même convention que les recherches de la bibliothèque standard. Une fonction qui rend une valeur
n'a pas ce luxe : zéro est une valeur plausible, d'où des conventions plus discutées. Retenir ce
critère — choisir la sentinelle *hors* du domaine de sortie — évite bien des réunions.

Le coût est linéaire en temps et constant en espace, et c'est une borne indépassable : déclarer
une position minimale sans avoir regardé toutes les autres serait un pari, pas un calcul. Trier
puis prendre la tête donnerait aussi la valeur minimale, mais perdrait la position d'origine et
coûterait un logarithme de plus — un bon exemple d'outil trop puissant pour la question posée.

La transposition est directe : « l'indice du meilleur candidat » est la forme générale de la
sélection — la ligne la plus ancienne d'un lot à purger, l'emplacement le moins chargé d'un
répartiteur, le prochain créneau libre d'un planning. Dans tous ces cas, la règle d'égalité fait
partie du contrat, et la comparaison stricte ou large doit être choisie, écrite et testée.
