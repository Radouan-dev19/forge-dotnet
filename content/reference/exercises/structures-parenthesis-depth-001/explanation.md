# Explication

Mesurer la profondeur maximale d'une imbrication combine deux exercices en un : la validation
d'équilibre du voisin, et le suivi d'un extremum au fil de l'eau. La combinaison crée sa propre
subtilité — une mesure n'a de sens que sur une structure valide — et c'est elle que le contrat
encode avec son verdict moins un.

Le suivi du pic d'abord. La profondeur courante monte à chaque ouvrante et descend à chaque
fermante ; le maximum ne peut donc progresser *qu'au moment d'une montée*, et c'est là, et
seulement là, que la solution le compare — `Math.Max(maximum, depth)` juste après
l'incrément. Relever le pic à chaque caractère serait correct et diluerait l'intention ; le
relever après la descente serait faux d'un niveau. Ce couplage mise-à-jour-puis-relevé est le
gabarit de tous les suivis d'extremum sur une quantité qui évolue : solde maximal atteint,
occupation de pointe d'une file, plus longue série en cours.

La validation ensuite, reprise du voisin mais avec une conséquence différente : là où
l'équilibre rendait un booléen, la mesure rend moins un — car « profondeur de quelque chose de
déséquilibré » n'existe pas, et rendre le pic observé avant la faute serait un mensonge de
mesure. Les deux fautes restent distinctes dans leur détection : la fermeture orpheline se voit
*immédiatement* — profondeur négative, retour sans lire la suite — tandis que l'ouverture jamais
refermée ne se voit qu'*à la fin* — profondeur finale non nulle. Le cas `((a)` est le piège
parfait : son pic vaut deux, sa structure est invalide, et une implémentation qui oublie le test
final rend deux avec assurance. Les cas cachés posent les deux fautes séparément pour vérifier
que chacune a son chemin.

La profondeur zéro appartient au domaine des réponses : un texte sans aucune parenthèse est
équilibré et plat — zéro, pas une erreur. La distinction entre « mesure nulle » et « mesure
impossible » est exactement ce que la sentinelle moins un exprime, choisie hors du domaine des
profondeurs valides — la même logique de sentinelle que les recherches d'indice.

Le coût est linéaire, l'espace constant : le compteur remplace la pile pour la même raison que
dans l'exercice d'équilibre — un seul type de délimiteur, la pile ne transporterait que sa
propre taille. La transposition suit la même paire : mesurer l'imbrication maximale de blocs, de
balises ou d'appels — pour détecter du code trop profond ou dimensionner un rendu — exige
d'abord d'en valider la structure, et le verdict d'invalidité doit rester distinct de toute
mesure légitime. Une métrique qui confond « zéro » et « incalculable » finit toujours par être
mal lue.
