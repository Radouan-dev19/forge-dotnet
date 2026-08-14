# Explication

Un tas rangé dans un tableau, où zéro marque l'absence : compter les nœuds présents se réduit à
compter les cases non nulles, et c'est cette *réduction* qui mérite l'explication — pourquoi une
question sur un arbre devient-elle une boucle plate ?

La représentation implicite d'un arbre binaire complet range les nœuds par niveaux : la racine à
l'indice zéro, les enfants de la case i aux cases 2i+1 et 2i+2. Aucun pointeur, aucune classe de
nœud — la *position* encode la parenté. C'est la représentation des tas binaires et des files de
priorité, et sa force est exactement ce que l'exercice montre : les questions globales — combien
de nœuds, quelle somme — se répondent par un parcours de tableau, sans récursion ni pile, parce
que la structure d'arbre n'ajoute rien à ces questions-là. Savoir *quand* la parenté importe —
pour naviguer, tamiser, comparer parent et enfant — et quand elle est du décor — pour compter —
évite bien des récursions inutiles.

La sentinelle mérite son paragraphe critique. Zéro signifie « absent » *dans cette représentation
pédagogique*, dit l'énoncé — et cette précaution de langage est une leçon en soi : une sentinelle
prise dans le domaine des valeurs interdit la valeur elle-même. Un tas qui devrait stocker des
zéros légitimes ne peut pas utiliser cette convention ; les représentations sérieuses portent
plutôt un compte de nœuds séparé, ou un type optionnel. L'exercice assume la sentinelle parce
qu'elle rend le contrat décidable en une comparaison, et il *déclare* la limite au lieu de la
cacher — le geste d'honnêteté que tout contrat à sentinelle devrait imiter.

Le comptage lui-même reprend le squelette parcours-prédicat-compteur : une comparaison à zéro
par case, un incrément conditionnel. Les valeurs négatives sont des nœuds *présents* — seul zéro
est absent, et le cas caché qui en mêle vérifie que le prédicat est bien `!= 0` et non `> 0`,
l'erreur de lecture la plus probable. Le tableau vide rend zéro par la boucle qui ne tourne
pas ; le tableau tout à zéro — que des absents — aussi, par le prédicat qui ne matche jamais :
deux chemins vers zéro, tous deux couverts.

Le coût est linéaire, incompressible — affirmer un compte exige de regarder chaque case —,
l'espace constant, l'entrée intacte puisque tout est lecture.

La transposition est double. Côté structure : reconnaître la représentation implicite quand on
la croise — tas, arbres de tournois, segments — et n'y naviguer que quand la question l'exige.
Côté convention : chaque sentinelle embarquée dans un domaine de valeurs doit être écrite,
justifiée et bornée à son contexte — sinon elle devient la valeur qu'on ne peut plus jamais
stocker, découvert le jour où le métier en a besoin.
