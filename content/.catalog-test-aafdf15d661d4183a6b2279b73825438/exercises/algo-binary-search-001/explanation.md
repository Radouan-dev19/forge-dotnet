# Explication

La recherche dichotomique n'est pas d'abord une boucle : c'est un invariant. À chaque tour, la
cible, si elle existe, se trouve dans l'intervalle fermé `[left, right]` — tout le reste du code
découle de cette phrase. La condition de sortie `left <= right` dit que l'intervalle contient
encore au moins un candidat ; les mises à jour `middle + 1` et `middle - 1` disent que l'élément
testé vient d'être éliminé et ne doit plus jamais être revu. C'est précisément là que les
implémentations naïves meurent : écrire `left = middle` sans le `+ 1` conserve un candidat déjà
testé, et sur un intervalle de deux éléments la boucle ne progresse plus. Le blocage n'apparaît
que sur certaines entrées, ce qui en fait un bug intermittent — la pire espèce.

Le calcul du milieu mérite sa ligne et son commentaire. La forme évidente `(left + right) / 2`
est correcte en mathématiques et fausse en machine : la somme de deux indices proches du maximum
d'un entier déborde avant la division. La forme `left + (right - left) / 2` calcule la même
valeur sans jamais former la somme dangereuse. Sur les tailles de tableau de cet exercice, le
débordement est impossible ; l'écrire quand même est le bon réflexe, parce que ce code-là sera
recopié un jour dans un contexte où les indices seront grands — c'est un bug historique célèbre,
resté vingt ans dans des bibliothèques standard.

Pourquoi pas un parcours linéaire, qui serait plus simple à écrire et passerait le cas nominal ?
Parce que le contrat de l'exercice est justement d'exploiter le tri. La dichotomie élimine la
moitié des candidats à chaque comparaison : trente-deux éléments demandent au plus six tours, un
million en demande vingt. Les cas cachés éprouvent les deux bornes du tableau et l'absence de la
cible — la valeur qui n'existe pas doit rendre `-1` après épuisement de l'intervalle, pas lever
ni boucler. Ils réfutent aussi la réponse codée en dur sur l'exemple visible, en déplaçant la
cible ailleurs dans le tableau.

Ce qui se transpose ailleurs : chaque fois qu'un espace de recherche est ordonné et qu'une
question se répond par « trop petit, trop grand ou trouvé », la même structure s'applique — un
seuil dans des données de mesure, la première version fautive dans un historique, une capacité
suffisante dans un dimensionnement. L'important n'est jamais la boucle, toujours l'invariant :
savoir dire ce qui est encore possible à chaque tour, et prouver que chaque tour réduit
strictement cet ensemble.
