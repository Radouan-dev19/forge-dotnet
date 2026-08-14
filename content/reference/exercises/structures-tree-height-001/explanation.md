# Explication

La hauteur d'un arbre encodé par parents se définit en une phrase : la plus longue chaîne d'un
nœud jusqu'à la racine, comptée en nœuds. La solution la transcrit littéralement — pour chaque
nœud, remonter et compter ; garder le maximum — et cette littéralité est un choix qu'il faut
savoir situer, car il en existe un autre.

La remontée par nœud recalcule des chemins partagés : deux feuilles sœurs remontent chacune
toute la chaîne commune. Le coût est donc, au pire, quadratique — un arbre filiforme fait
remonter chaque nœud sur toute sa profondeur. La version savante mémorise la profondeur de
chaque nœud déjà résolu et la réutilise : chaque lien n'est alors parcouru qu'une fois, coût
linéaire, au prix d'un tableau auxiliaire et d'une logique de remplissage à deux temps. Sur les
tailles de l'exercice, la version directe gagne par sa simplicité de preuve ; savoir *décrire*
la mémoïsation et son seuil de rentabilité est le niveau attendu, l'écrire viendra avec les
arbres réels. Cette gradation — d'abord l'algorithme évident et prouvable, ensuite le cache
quand les données l'exigent — est une trajectoire de conception à part entière.

La garde anti-cycle reprend l'argument de comptage du voisin compteur d'ancêtres : une chaîne
sans répétition dans un tableau de n nœuds fait au plus n pas, donc le pas n plus un prouve le
cycle et la fonction rend moins un — les données ne décrivent pas un arbre, aucune hauteur
n'existe. Le compteur `guard` est distinct de `depth` : l'un mesure, l'autre surveille, et les
confondre rendrait le verdict dépendant du point de départ de la remontée fautive.

La convention de comptage se lit dans l'initialisation : `depth` part de un — la racine seule a
une hauteur de un, pas zéro. Les deux conventions existent dans la littérature ; l'exemple de
l'énoncé tranche — trois nœuds sur deux niveaux, hauteur deux — et le cas caché de la racine
isolée vérifie l'ancrage. Le tableau vide rend zéro par la boucle externe qui ne tourne pas :
aucun nœud, hauteur nulle, cohérent sans garde dédiée.

Les cas cachés composent les régimes : l'arbre filiforme — hauteur égale au nombre de nœuds —,
l'arbre plat — tout le monde accroché à la racine, hauteur deux —, le cycle qui doit rendre
moins un, et une forme inédite contre la réponse figée.

La transposition rejoint celle des remontées de graphe : profondeur d'une hiérarchie de
dossiers, niveau d'un employé dans un organigramme, degré d'imbrication d'une catégorie — même
encodage par parents, même remontée, même garde. Et le même réflexe d'évolution : quand les
mêmes chemins se recalculent trop, le cache de profondeurs est la première optimisation à
proposer.
