# Explication

La validité d'un champ n'est qu'une des trois questions qu'une interface se pose à son sujet, et les
confondre produit les formulaires les plus agaçants. La première dimension, propre ou modifié, dit
si l'utilisateur a saisi quelque chose. La deuxième, non touché ou touché, dit s'il a quitté le
champ au moins une fois. La troisième, valide ou invalide, dit si le contenu satisfait les règles.
Ces axes sont volontairement séparés parce qu'ils répondent à des besoins différents de rendu.

Le piège classique, et ce que le premier cas visible vérifie, est d'afficher une erreur avant tout
contact. Un champ requis est invalide dès l'ouverture, puisqu'il est vide ; mais bombarder
l'utilisateur d'un message rouge sur un formulaire qu'il vient à peine d'ouvrir est hostile.
L'interface s'appuie donc sur la marque de contact pour décider quand montrer l'erreur : la validité
existe dès le départ, son affichage attend que le champ ait été touché. En gardant ces deux
informations distinctes, la méthode laisse la couche de rendu combiner ce qu'elle veut, au lieu de
figer une politique d'affichage dans le calcul de l'état.

C'est ici que se joue la différence de fond avec une validation de charge utile côté serveur, qui
reçoit un ensemble de champs déjà soumis et agrège toutes les violations d'un coup. Notre problème
est antérieur : il vit dans le navigateur, suit le geste de l'utilisateur événement par événement,
et son enjeu n'est pas la liste des fautes mais le bon moment pour parler. Un champ peut être
invalide et pourtant ne rien afficher, simplement parce qu'il n'a pas encore été touché.

Le `reset` mérite une attention particulière, et un cas caché le cible. Beaucoup d'implémentations
n'effacent que la valeur et oublient la marque de contact. Le résultat est un champ vidé qui
continue d'afficher son erreur, comme s'il gardait rancune d'avoir été touché. Ramener ensemble la
valeur initiale et l'état non touché est la seule façon de rendre au champ son innocence de départ.

La règle `minlen` illustre une autre subtilité : elle ne s'applique qu'à une valeur non vide. Sur un
champ facultatif laissé vide, imposer une longueur minimale recalerait à tort un champ que
l'utilisateur avait le droit de ne pas remplir. La longueur est une contrainte sur ce qui est écrit,
pas une obligation d'écrire ; c'est `required` qui porte cette obligation, et les deux ne doivent pas
se marcher dessus.

Le coût de tout cela est linéaire dans le nombre d'événements, puisqu'on les rejoue une fois. La
transposition dépasse le formulaire : toute machine à états d'interaction, d'un bouton qui se désarme
après un clic à un flux d'authentification à étapes, vit du même principe, garder des dimensions
d'état orthogonales et ne décider de l'affichage qu'à la toute fin.
