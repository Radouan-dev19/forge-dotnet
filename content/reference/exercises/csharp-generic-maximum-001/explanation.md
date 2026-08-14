# Explication

Cette recherche de maximum illustre le squelette générique de toute *réduction* — un
accumulateur, un parcours, une règle de combinaison — et la spécialise avec deux décisions de
contrat qui font la différence entre une fonction sûre et une fonction plausible.

La décision d'initialisation d'abord, la plus instructive du lot. L'accumulateur part de
`values[0]`, une donnée réelle, et non d'une constante comme zéro. La version initialisée à zéro
est correcte sur toutes les entrées contenant au moins une valeur positive ou nulle — donc sur
tous les exemples spontanés — et rend zéro sur un tableau entièrement négatif, une valeur qui
n'existe pas dans les données. Ce motif d'erreur, l'accumulateur étranger aux données, dépasse
le maximum : une concaténation initialisée à une chaîne fantôme, un « meilleur candidat » parti
d'un objet par défaut — chaque réduction pose la même question, et la même réponse vaut partout.
Ancrer le point de départ dans les données rend le résultat membre des données par récurrence,
quel que soit leur signe. Le parcours peut alors relire la première case sans dommage — se
comparer à soi-même ne change rien — ce qui permet le `foreach` intégral, plus simple qu'une
boucle démarrée à l'indice un.

La décision de repli ensuite. Le maximum d'un ensemble vide n'existe pas ; le contrat tranche
par zéro et l'inscrit dans le nom même de la méthode — `MaximumOrZero` — ce qui est la façon la
plus honnête de porter une convention : l'appelant la lit dans la complétion de son éditeur,
pas dans une documentation qu'il n'ouvrira pas. L'alternative de la bibliothèque, `Max()` de
LINQ, lève sur une séquence vide ; les deux conventions coexistent dans du code réel, et le
danger n'est jamais l'une ou l'autre, c'est l'implicite. Ici, `null` et vide partagent le même
repli, choix défendable pour une fonction d'agrégation tolérante — le régime strict qui
distingue la faute d'appel appartient à d'autres exercices du catalogue, et comparer les deux
régimes fait partie de l'apprentissage.

Les cas cachés visent chaque décision : le tableau tout négatif réfute l'initialisation à zéro,
le vide vérifie le repli, le maximum en première et en dernière position encadre le parcours, et
une disposition inédite réfute la réponse figée. Le coût est linéaire, incompressible — affirmer
un maximum exige d'avoir tout vu — et l'espace est constant.

La transposition est le questionnaire de toute réduction : d'où part l'accumulateur, que rend
l'agrégat vide, la règle de combinaison est-elle associative ? Trois réponses écrites, et
n'importe quelle somme, moyenne, ou sélection de « meilleur » se code sans surprise.
