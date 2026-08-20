# Explication

Un décalage d'une unité sur une borne est la faute la plus fréquente d'une règle numérique, et la plus
difficile à voir en relecture. Le code compile, il se lit correctement, et il se comporte comme prévu
partout sauf sur une seule valeur. Un test qui se contente de vérifier le milieu de l'intervalle
passera au vert et ne prouvera rien de la frontière.

**Pourquoi quatre valeurs, et pourquoi celles-là.** Chaque borne peut être fausse de deux façons :
trop large, et elle accepte une valeur qu'elle devrait refuser ; trop stricte, et elle refuse une
valeur qu'elle devrait accepter. La première sonde extérieure réfute la version trop large, la
première sonde intérieure réfute la version trop stricte. Deux bornes, deux erreurs possibles chacune,
quatre sondes. Aucune n'est redondante, et aucune ne manque : tester le milieu n'ajoute rien, tester
deux valeurs au-delà de la borne n'ajoute rien non plus, car la même comparaison décide pour toutes.

**Deux bornes confondues ne produisent pas quatre sondes.** L'intervalle ne contient qu'une valeur, et
c'est à la fois la première et la dernière valeur acceptée. La produire deux fois n'ajoute aucune
capacité de réfutation ; cela gonfle seulement le compte de cas, et un compte de cas qui gonfle sans
réfuter davantage est exactement ce qui fait croire une suite plus solide qu'elle ne l'est.

**La limite du type est une frontière comme une autre, et c'est le vrai piège du sujet.** Une borne
posée sur la plus petite valeur du type n'a pas d'extérieur : la valeur d'en dessous n'existe pas.
Calculer la sonde sans le vérifier ne produit pas une erreur visible mais un débordement silencieux,
et la sonde atterrit à l'autre extrémité du type. Le test sonderait alors la plus grande valeur en
croyant sonder la plus petite — une sonde qui vise l'inverse de son intention, ce qui est pire qu'une
sonde absente. Omettre la sonde et le dire est la seule réponse honnête.

**Un intervalle vide est refusé plutôt que rendu sans sonde.** Rendre un tableau vide serait défendable
et cache le problème : l'appelant obtient une liste de cas de longueur zéro, sa boucle de test ne
s'exécute jamais, et sa suite reste verte en n'ayant rien vérifié. Un intervalle dont la borne basse
dépasse la borne haute est une erreur de spécification en amont ; la signaler à l'endroit où elle est
détectée évite qu'elle produise une suite silencieusement vide.

Le coût est constant : quatre sondes au plus, quelle que soit la largeur de l'intervalle. C'est la
propriété qui rend cette technique applicable partout, y compris sur des intervalles de plusieurs
milliards de valeurs.
