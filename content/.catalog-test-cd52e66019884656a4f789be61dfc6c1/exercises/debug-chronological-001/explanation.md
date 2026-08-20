# Explication

Vérifier qu'une série d'horodatages est en ordre : c'est la première question qu'un débogueur de
journaux pose — les événements sont-ils arrivés dans l'ordre où ils sont écrits ? — et sa
réponse correcte contient trois décisions que la solution rend visibles.

La première est la définition de « chronologique » : les égalités passent. Deux événements dans
la même seconde sont un fait ordinaire des journaux réels — la granularité de l'horloge est
finie — et exiger une croissance stricte déclarerait désordonnés des journaux parfaitement
sains. Le prédicat de rejet est donc `timestamps[i] < timestamps[i - 1]` — une vraie
*inversion* — et non `<=`. L'exemple de l'énoncé contient le doublon exprès, et le cas caché
qui n'en contient pas départage l'écriture stricte de la large dans l'autre sens.

La deuxième est la méthode : comparer chaque élément à son prédécesseur, sans trier. Trier une
copie puis comparer à l'original répondrait aussi — au prix d'un logarithme de plus et d'une
allocation — mais surtout, cette approche répond à une *autre question* : « ces valeurs
pourraient-elles être en ordre ? » au lieu de « sont-elles en ordre ? ». Le parcours par paires
voisines est la transcription directe de la définition — une série est ordonnée si aucune paire
adjacente n'est inversée — et l'énoncé interdit le tri précisément pour forcer cette
transcription. C'est le même squelette voisin-à-voisin que le comptage de groupes : l'état
utile est déjà dans le tableau, à l'indice moins un.

La troisième est la sortie précoce : `return false` à la *première* inversion. Un journal de
production peut être immense, et le verdict est acquis dès la première faute — continuer serait
du travail mort. La conséquence est un profil de coût asymétrique à savoir énoncer : linéaire au
pire — la série ordonnée doit être lue en entier pour être déclarée telle —, souvent bien plus
court sur les séries fautives.

Les bornes se traitent par vacuité : le tableau vide et le singleton sont ordonnés — aucune
paire à vérifier, la boucle qui démarre à un ne tourne pas — et aucune garde spéciale n'est
nécessaire. Les horodatages négatifs se comparent comme les autres : le prédicat ne suppose
rien sur les signes.

La transposition est la routine du diagnostic : avant d'accuser un traitement d'être fautif,
vérifier que ses entrées respectent l'ordre qu'il suppose — messages d'une file, lignes d'un
export, migrations d'un schéma. Cette vérification-là coûte un parcours et évite des heures
d'enquête sur un algorithme innocent ; l'écrire proprement, égalités comprises, est le premier
geste du débogage de flux.
