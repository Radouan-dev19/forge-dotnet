# Explication

Un budget d'imbrication : zéro à trois niveaux acceptés, au-delà refusé. Le prédicat est une
plage — la mécanique est connue — et la valeur de l'exercice est dans la *métrique* qu'il fait
défendre : pourquoi la profondeur d'imbrication est-elle une mesure de qualité, et pourquoi la
borner ?

L'énoncé demande ce qu'une imbrication excessive rend difficile à tester, et la réponse se
calcule : chaque niveau de condition imbriquée multiplie les chemins d'exécution. Trois
niveaux, c'est déjà jusqu'à huit chemins à couvrir ; cinq niveaux, trente-deux — et une suite
de tests qui prétend couvrir une méthode à cinq étages d'imbrication ment presque toujours.
La profondeur est aussi une mesure de charge mentale : le lecteur au fond d'un quatrième `if`
doit tenir quatre contextes vrais simultanément pour comprendre la ligne qu'il lit. Le budget
n'est pas un dogme esthétique, c'est un plafond de testabilité — et les remèdes au dépassement
sont un répertoire connu : gardes à retour précoce qui aplatissent les cas d'erreur,
extraction de méthodes qui remet chaque niveau à zéro, inversion de conditions. Les solutions
de ce catalogue les pratiquent systématiquement, et ce prédicat est la règle qui les
industrialiserait dans une chaîne de qualité.

Le domaine de la mesure mérite ses bornes des deux côtés, et c'est la subtilité du contrat. En
haut, le budget : trois inclus, quatre refusé — la frontière de politique, ajustable par
équipe, mais toujours *quelque part*. En bas, zéro inclus — une méthode sans aucune
imbrication est la meilleure élève du lot — et le *négatif refusé* : une profondeur négative
ne mesure rien, c'est un défaut de l'outil de mesure amont, et le prédicat répond faux plutôt
que d'avaler l'absurde. Ce refus des mesures incohérentes distingue un prédicat de politique —
qui juge des mesures supposées valides — d'un simple comparateur : les données d'entrée d'une
chaîne de qualité ont elles-mêmes des bugs, et les laisser passer fabrique des rapports verts
sur des mesures fausses.

La forme `is >= 0 and <= 3` reprend le motif de plage lisible, ses quatre frontières
testables : zéro et trois qui passent, moins un et quatre qui échouent — le plan de test
s'écrit tout seul, et les cas cachés le déroulent.

Le coût est constant. La transposition est le principe des *budgets de qualité chiffrés* :
profondeur, longueur de méthode, nombre de paramètres, complexité cyclomatique — chaque
métrique mérite son seuil explicite, vérifié par l'outillage, plutôt qu'un « c'est trop
complexe » d'appréciation. Un budget chiffré se discute, s'ajuste et se fait respecter ; un
goût personnel se renégocie à chaque revue.
