# Explication

La recherche linéaire semble trop simple pour mériter une explication ; c'est justement parce
qu'elle est simple qu'on y voit nettement trois contrats distincts, que les exercices suivants
recombineront sans cesse.

Le premier contrat distingue deux absences qui n'ont rien en commun. Un tableau `null` est une
faute de l'appelant — il n'a pas fourni de collection du tout — et se signale par
`ArgumentNullException`, immédiatement et nommément. Une valeur introuvable dans un tableau bien
réel est au contraire un résultat ordinaire de la recherche, et se signale par la convention
`-1`, celle de `IndexOf` dans toute la bibliothèque standard. Confondre les deux — retourner `-1`
sur `null`, ou lever sur une valeur absente — fabrique soit un silence qui masque un bug d'appel,
soit une exception pour un cas parfaitement normal. Savoir ranger chaque situation dans la bonne
catégorie est la compétence que cet exercice installe.

Le deuxième contrat est le mot « première ». Le parcours va de gauche à droite et s'arrête à la
rencontre : sur un tableau qui contient la cible deux fois, c'est l'indice le plus petit qui
sort. Un `return` au milieu d'une boucle est exactement le bon outil ici — continuer le parcours
pour retourner la dernière occurrence serait un autre contrat, et mémoriser toutes les positions
un troisième. Les cas cachés placent des doublons pour vérifier lequel des trois a été écrit, et
déplacent la cible en tête, en queue et hors du tableau pour éprouver les bornes du parcours.

Le troisième contrat est le coût, et il faut le dire sans complexe : linéaire, sans autre
hypothèse. La recherche dichotomique fait mieux, mais elle exige un tableau trié — une promesse
que l'appelant doit tenir et que rien ne vérifie. Sur des données non triées, non indexées, la
recherche linéaire est l'optimum : il faut bien regarder chaque case au moins une fois pour
affirmer qu'une valeur n'y est pas. La vraie décision d'ingénierie n'est donc jamais « linéaire
ou dichotomique » dans l'absolu, mais « combien de recherches ferai-je sur ces données » : une
seule recherche ne justifie ni tri ni index, mille recherches justifient l'un ou l'autre.

Cette hiérarchie — contrat d'erreur, contrat de résultat, contrat de coût — se transpose telle
quelle aux fonctions de dépôt de données : que fait `Find` d'un identifiant `null`, que rend-il
quand rien ne correspond, et que promet-il quand la table grossit ? Les trois réponses doivent
être écrites, et cet exercice est leur première rédaction.
