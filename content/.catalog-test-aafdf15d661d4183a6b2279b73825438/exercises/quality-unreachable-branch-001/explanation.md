# Explication

Une branche morte n'est pas seulement du code inutile. C'est une intention qui n'a jamais été
exécutée : quelqu'un a écrit une règle, l'a crue active, et le programme ne l'a jamais appliquée. Un
test qui viserait cette branche passerait sans jamais y entrer. C'est précisément ce que cherche un
analyseur statique, et le faire une fois à la main change la façon de lire ses avertissements.

**Comparer les conditions deux à deux ne suffit pas, et c'est le piège central.** Une branche peut
être atteignable face à chacune de ses devancières prise isolément, et morte face à leur réunion. Deux
conditions qui découpent le domaine en deux moitiés en couvrent la totalité : n'importe quelle
troisième condition est alors morte, sans qu'aucune des deux ne la contienne à elle seule. Le
raisonnement doit donc porter sur l'ensemble cumulé, jamais sur des paires.

**D'où la représentation en réunion d'intervalles, triée et sans chevauchement.** Elle rend la
question décidable en un seul passage : on place un curseur sur la plus petite valeur que le candidat
accepte, et chaque intervalle couvert qui contient ce curseur le repousse juste après sa propre borne
haute. Si le curseur franchit la borne haute du candidat, tout ce que le candidat accepte était déjà
couvert. S'il se bloque, la valeur sur laquelle il se bloque est un témoin concret d'atteignabilité —
et c'est ce témoin qu'on voudrait afficher dans un vrai analyseur.

**La contiguïté est le détail qui décide de la justesse.** Deux intervalles qui se touchent bout à
bout, comme « jusqu'à dix inclus » et « au-delà de dix », ne laissent aucune valeur entre eux. Si la
fusion ne traite que le chevauchement et pas la contiguïté, la réunion garde un trou de largeur nulle,
le curseur s'y arrête, et une branche morte est déclarée vivante. C'est le genre de faute qui ne se
voit sur aucun cas simple et apparaît sur le premier cas réel.

**Le débordement est l'autre piège, et il est silencieux.** Traduire « strictement inférieur à » exige
la valeur juste en dessous de la borne, et « strictement supérieur à » la valeur juste au-dessus.
Quand la borne est déjà la plus petite ou la plus grande du type analysé, ces valeurs n'existent pas
dans ce type. Calculer dans le même type fait basculer la borne à l'autre extrémité et transforme un
intervalle vide en intervalle qui couvre presque tout. Mener les calculs dans un type plus large
supprime le problème à la racine plutôt que de le rattraper par des cas particuliers.

**Une condition que le domaine ne peut pas satisfaire est morte par elle-même.** Elle n'a besoin
d'aucune devancière pour l'être : son ensemble d'acceptation est vide. C'est une catégorie distincte
et elle mérite d'être traitée avant la recherche de couverture, sinon le curseur travaille sur un
intervalle inversé et le résultat n'a plus de sens.

Le coût est quadratique dans le pire cas, parce que chaque insertion peut fusionner toute la liste.
Sur une cascade de conditions écrite par un humain, la liste reste courte : la lisibilité de la
fusion vaut mieux ici qu'une structure ordonnée plus savante.
