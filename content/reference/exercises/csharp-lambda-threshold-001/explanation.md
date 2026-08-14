# Explication

Une seule ligne utile — `values.Count(value => value >= minimum)` — mais elle assemble trois
notions que l'exercice veut rendre conscientes : le prédicat comme valeur, la capture de
variable, et la surcharge qui fusionne deux opérations.

Le prédicat d'abord. `value => value >= minimum` est une fonction anonyme dont le type est
`Func<int, bool>` : elle reçoit un élément, répond oui ou non. L'écrire en lambda plutôt qu'en
méthode nommée la place là où elle se lit — au point d'usage — et c'est le bon choix tant
qu'elle tient en une expression ; dès qu'un prédicat se complique ou se réutilise, le promouvoir
en méthode nommée redevient préférable. Ce critère de bascule, taille et réutilisation, est plus
utile que n'importe quel dogme sur les lambdas.

La capture ensuite, discrète et fondamentale : `minimum` n'est pas un paramètre de la lambda,
c'est une variable de la méthode englobante que la lambda *capture*. Le compilateur fabrique la
glu — une fermeture — pour que le prédicat emporte son seuil avec lui. C'est ce mécanisme qui
rend les prédicats paramétrables sans variable globale ni état partagé, et c'est lui qu'on
retrouvera dans chaque `Where`, chaque gestionnaire d'événement, chaque configuration propre à
une requête. Le comprendre ici, sur un entier, épargne de le découvrir dans un bug de capture de
variable de boucle.

La surcharge enfin. `Count(prédicat)` répond en un seul parcours à la question « combien
satisfont ? ». La version en deux temps — `Where(...).Count()` — rend le même nombre avec un
étage d'énumération de plus ; la version fautive `Where(...).ToArray().Length` matérialise un
tableau entier pour le jeter aussitôt compté. Aucune des trois n'est fausse ; une seule ne paie
que ce qu'elle consomme, et savoir repérer les allocations sans usage est exactement le genre de
lecture qu'une revue attend.

Le contrat répète les clauses désormais familières, et leur répétition est voulue — c'est un
vocabulaire : borne incluse par `>=`, cas posé sur le seuil dans les cachés ; `null` en faute
d'appel ; tableau vide qui rend zéro par parcours nul ; entrée jamais modifiée, un comptage ne
faisant que lire. Le seuil peut être négatif ou dépasser toutes les valeurs — zéro survivant est
une réponse, pas une anomalie.

Le coût est un parcours linéaire sans allocation. La transposition : compter au lieu de
collecter chaque fois que seul le nombre importe — lignes en erreur, clients éligibles, tâches
en retard — et laisser le prédicat porter la règle métier, paramétrée par capture. Quand la
question devient « lesquels ? », alors seulement `Where` entre en scène.
