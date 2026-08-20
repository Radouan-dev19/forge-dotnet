# Explication

Une remise à paliers — rien sous cent, cinq pour cent jusqu'à deux cents, quinze au-delà — et
une question de test : comment couvrir cette règle pour qu'aucune réécriture fautive ne passe ?
L'exercice est un barème *et* son plan de test, et c'est le second qui porte le nom de la
famille.

La technique s'appelle le *partitionnement d'équivalence* : le domaine des montants se découpe
en classes où la règle se comporte pareil — sous cent, entre cent et deux cents, au-delà — et
une valeur par classe suffit à exercer le comportement de la classe. Trois cas, donc ? Non, et
c'est le point que l'énoncé fait nommer : un test qui ne touche aucun palier ne peut pas
détecter un *déplacement de frontière*. La règle réécrite avec `> 100` au lieu de `>= 100` a
les mêmes trois classes à un point près — le palier exact — et les valeurs intérieures des
classes passent identiquement. La couverture honnête combine donc les deux techniques : une
valeur par partition *plus* les frontières exactes avec leurs voisines — cent qui donne cinq
pour cent, quatre-vingt-dix-neuf qui donne zéro, deux cents qui donne quinze, cent
quatre-vingt-dix-neuf qui donne cinq. Partition pour le comportement, frontière pour la
position : ni l'une ni l'autre seule ne suffit.

Le code transcrit le barème par gardes ordonnées *du haut vers le bas* : le palier le plus
exigeant se teste en premier, et chaque garde suivante travaille sur le domaine résiduel. Ce
sens de lecture — l'inverse du classement d'âges qui montait — est le naturel des barèmes à
seuils d'accès : « au moins deux cents » se lit avant « au moins cent », sinon la deuxième
garde happerait tout et le taux plein deviendrait inatteignable — l'erreur d'ordre classique,
qu'un seul cas à deux cent cinquante suffit à réfuter. L'inclusivité des seuils — `>=` — est
la clause commerciale : atteindre le palier donne le taux du palier.

Le montant négatif lève avant tout barème, invariant du domaine monétaire. Les taux sont des
littéraux décimaux exacts — des fractions, prêtes à multiplier un total ailleurs — et la
fonction rend le *taux*, pas le montant remisé : la séparation règle-contre-application déjà
vue, qui rend le barème testable par table.

Le coût est constant. La transposition est le duo de techniques lui-même, applicable à tout
barème — frais de port par tranches, commissions par volume, niveaux de service par
ancienneté : découper en partitions, poser une valeur par partition, puis encadrer chaque
palier de son triplet. Le plan de test se dessine *avant* le code, et c'est lui, pas le code,
que l'exercice évalue vraiment.
