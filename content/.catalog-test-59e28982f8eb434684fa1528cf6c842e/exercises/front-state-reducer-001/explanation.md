# Explication

Un réducteur transforme un état en un nouvel état sous l'effet d'une action, et sa promesse tient
en un mot : il ne touche jamais à l'ancien. Cette discipline paraît théorique tant qu'on code seul
sur une seule variable ; elle devient vitale dès qu'une interface partage la même donnée entre
plusieurs vues. Si la méthode modifiait la table reçue, tout composant qui en détient encore une
référence verrait son contenu bouger sans avoir rien demandé, et le rendu afficherait un état que
personne n'a explicitement produit. C'est la source de bogues la plus déroutante du côté client :
un changement apparaît là où aucun code local ne l'a écrit. Construire une table neuve ferme cette
porte, parce que l'ancienne valeur reste figée et comparable à la nouvelle.

Les cas cachés sondent précisément les endroits où la mutation ou une conversion trop zélée se
glisse. L'incrément répété vérifie que la lecture puis l'écriture d'un entier fonctionnent en
chaîne, sans dérive. Le cas de la valeur textuelle soumise à un incrément est le plus révélateur :
une implémentation naïve tenterait la conversion, échouerait, et soit lèverait une exception, soit
écrirait un zéro par défaut. La règle exige au contraire d'abandonner l'action et de préserver la
valeur d'origine intacte. Ce choix protège l'utilisateur d'une perte silencieuse de donnée : une
étiquette ne devient jamais un nombre parce qu'un message mal formé a demandé de l'augmenter.

Le coût d'une erreur ici se mesure en confiance. Un état corrompu à cause d'un partage de référence
ne se reproduit pas de façon fiable, ne s'attrape pas au débogueur sans peine, et sème le doute sur
tout le reste du code. À l'inverse, un réducteur pur se teste par simple comparaison entrée-sortie,
se rejoue à l'identique, et se compose sans surprise avec d'autres réducteurs.

Le tri final en ordre ordinal n'est pas cosmétique. Une sortie ordonnée de façon stable, insensible
à la culture de la machine, rend deux exécutions comparables et permet à un test de vérifier une
chaîne exacte plutôt qu'un ensemble sans ordre. Trier selon la culture courante rendrait le
résultat dépendant du système, et un test vert sur une machine deviendrait rouge sur une autre.

La transposition dépasse le formulaire. Tout gestionnaire d'état sérieux, du panier d'achat à
l'historique annulable, repose sur ce même contrat : une fonction qui reçoit l'ancien état et une
intention, et rend un état neuf sans jamais abîmer l'ancien. Maîtriser ce contrat sur un cas de
quelques lignes, c'est acquérir le réflexe qui tient à grande échelle.
