# Explication

Extraire une extension de fichier semble être un problème de découpage de chaîne ; la solution
n'écrit pourtant aucun découpage, et ce refus est la première leçon. `Path.GetExtension`
appartient à la bibliothèque standard, connaît les cas que l'artisanat oublie — le fichier sans
extension, le nom qui finit par un point, le chemin avec des répertoires qui contiennent
eux-mêmes des points — et rend l'extension *avec* son point de tête, convention que l'exemple de
l'énoncé confirme. Réécrire cette logique avec `LastIndexOf('.')` produit une version qui passe
le cas nominal et se trompe sur `archive.tar.gz` — la *dernière* extension est `.gz`, pas
`.tar.gz` — ou sur `dossier.v2/rapport`, où le point appartient au répertoire. Utiliser l'outil
du domaine plutôt que l'outil générique des chaînes : c'est le réflexe que l'exercice installe,
et il vaut pour les dates, les URL et les chemins bien au-delà de ce cas.

La deuxième décision est la normalisation de casse, et son suffixe. Les extensions servent de
clés de décision — router vers un traitement, filtrer une liste, valider un envoi — et
`report.JSON` doit suivre le même chemin que `report.json`. `ToLowerInvariant` fige la règle
indépendamment de la culture de la machine : la casse culturelle réserve des surprises célèbres,
et une clé de routage qui varie selon la configuration du serveur est un incident en attente.
Minuscule invariante pour comparer, casse d'origine pour afficher — la solution choisit la
première parce que le contrat produit une valeur de comparaison.

La garde d'entrée regroupe `null`, vide et blanc en un seul repli : la chaîne vide. C'est une
convention d'affichage et de filtrage — « pas d'extension » — cohérente avec ce que
`GetExtension` rend déjà pour un fichier sans point. L'exception se défendrait dans un autre
contexte ; ici, la fonction alimente des comparaisons, et une valeur neutre compose mieux
qu'un incident.

Les cas cachés balaient ce que la bibliothèque sait et que l'artisanat rate : nom sans point,
casse mélangée, double extension dont seule la dernière sort, et le repli des entrées blanches.
Le coût est linéaire dans la longueur du chemin, avec une allocation pour la normalisation.

La transposition tient en une question et un ordre : existe-t-il déjà, dans la bibliothèque, un
type ou une fonction qui *comprend* ce format ? Si oui, l'utiliser, puis normaliser le résultat
pour l'usage prévu — jamais l'inverse, et jamais à la main ce que le domaine offre déjà.
