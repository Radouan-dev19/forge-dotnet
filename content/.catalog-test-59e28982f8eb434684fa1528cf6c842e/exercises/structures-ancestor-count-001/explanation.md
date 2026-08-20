# Explication

Un arbre encodé dans un tableau : chaque case donne le parent de son indice, la racine porte
moins un, et compter les ancêtres revient à suivre la chaîne jusqu'à elle. Le parcours tient en
une boucle ; ce qui fait l'exercice, c'est que l'entrée n'est *pas* forcément un arbre — et
qu'une fonction qui suppose la bienveillance des données boucle pour toujours.

Le danger central est le cycle. Si deux nœuds se désignent mutuellement comme parents — donnée
corrompue, jamais un arbre légitime — la remontée naïve ne rencontre jamais la racine et la
boucle tourne sans fin. La parade de la solution est un argument de comptage, à savoir énoncer :
dans un tableau de n cases, une chaîne d'ancêtres sans répétition compte au plus n pas ; le
pas numéro n plus un prouve donc qu'un nœud a été revisité, c'est-à-dire un cycle. Le compteur
qui sert déjà à la réponse fait office de garde-fou — `++count > parents.Length` — sans
structure auxiliaire. L'alternative classique mémorise les nœuds visités dans un ensemble :
détection au premier tour de cycle au prix d'un espace linéaire ; ici, la précision du point de
détection n'a aucune valeur — on rend moins un dans tous les cas — donc la borne gratuite gagne.
Comparer ces deux parades, et savoir quand chacune se justifie, est le vrai contenu de
l'exercice.

Même discipline pour les indices : le nœud de départ est validé avant tout accès, et *chaque
parent lu* est validé avant d'être suivi — un tableau qui désigne la case quarante-deux dans un
tableau de trois est une corruption à refuser, pas une exception d'indice à laisser fuser. Le
contrat unifie toutes les anomalies — nœud invalide, parent hors bornes, cycle — dans la même
réponse moins un : une convention de verdict, cohérente pour l'appelant qui veut juste savoir si
la généalogie est saine et profonde de combien.

Le chemin nominal, lui, est simple : la racine répond zéro — aucun pas, la boucle ne tourne
pas — et l'exemple à deux ancêtres suit deux liens. Les cas cachés placent le cycle, le nœud
hors bornes, le parent corrompu et la racine elle-même : chaque garde a son cas, aucune n'est
décorative.

Le coût est linéaire dans la profondeur, borné par la taille du tableau grâce au garde-fou ;
l'espace est constant. La transposition est partout où des données se référencent : chaînes de
délégation, hiérarchies de dossiers, liens « répond à » d'une messagerie — toute remontée de
graphe fourni par l'extérieur doit borner ses pas ou mémoriser ses visites, parce que les
données réelles contiennent des cycles que les schémas juraient impossibles.
