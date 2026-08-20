# Explication

Cet exercice a l'air de porter sur une division ; il porte en réalité sur la définition exacte de
ce qu'on compte. « Le nombre de réductions réellement faites » impose trois décisions que la
boucle matérialise, et c'est sur elles que les cas cachés appuient.

La première est la borne de la boucle. `value > 1` et non `value > 0` : une fois arrivé à un, il
n'y a plus de réduction à faire — diviser un par deux donne zéro en arithmétique entière, ce qui
serait une étape de plus qui ne correspond à rien dans l'énoncé. Le hors-par-un se joue
entièrement dans ce choix de comparaison, et l'erreur produit un résultat décalé d'exactement une
unité sur toutes les entrées, ce qu'un seul cas bien choisi suffit à réfuter. Les entrées zéro et
un rendent zéro : aucune division n'a lieu, et la boucle qui ne s'exécute pas est la bonne
réponse, pas un cas spécial à coder.

La deuxième est le refus du négatif. Le contrat ne définit pas de comportement pour une valeur
négative, et la boucle écrite naïvement n'y terminerait même pas — un négatif divisé par deux
reste négatif et ne franchit jamais la borne. Entre inventer une convention silencieuse et
signaler l'entrée hors domaine, la solution choisit `ArgumentOutOfRangeException` : l'appelant
apprend immédiatement qu'il a fourni une valeur sans signification pour cette fonction, au lieu
de recevoir un nombre plausible et faux. Lever tôt sur un domaine non défini est une décision qui
se transpose à presque toutes les fonctions de calcul.

La troisième est la nature du résultat : ce compteur est le logarithme en base deux, arrondi vers
le bas, de la valeur d'entrée — huit demande trois réductions, mille en demande neuf. C'est
exactement le nombre de tours d'une recherche dichotomique sur un espace de cette taille, et
c'est pour cela que l'exercice existe : rendre tangible ce que « logarithmique » veut dire, en le
comptant à la main. Quand une revue annonce qu'un traitement est en `O(log n)`, elle affirme que
doubler les données n'ajoute qu'un tour ; cette intuition-là, une fois construite ici, se
réutilise pour dimensionner des index, des arbres et des stratégies de réessai.

Le coût de la fonction est son propre sujet : logarithmique en temps, constant en espace. Une
variante récursive serait aussi correcte et consommerait de la pile pour rien ; la boucle est ici
la forme la plus simple qui dit ce qu'elle fait.
