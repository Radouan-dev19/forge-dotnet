# Explication

Trois problèmes distincts se cachent derrière une simple option de tri, et une implémentation naïve
les manque tous les trois.

**Le premier est un problème de sécurité.** Le tri demandé finit dans une clause d'ordre. Concaténer
le texte reçu, c'est laisser le client écrire une partie de la requête. La liste blanche n'est donc
pas un confort d'ergonomie : c'est la frontière qui fait que seule une valeur choisie par le serveur
traverse. Rendre l'orthographe déclarée plutôt que celle reçue n'est que la conséquence de ce choix
— ce qui sort de la fonction vient du serveur, jamais du client.

**Le deuxième est un problème d'honnêteté.** Que faire d'un champ hors liste blanche ? L'ignorer est
tentant : la requête reste valide et le client reçoit ses données. Mais il les croit triées comme il
l'a demandé, alors qu'elles ne le sont pas. Il paginera dessus, comparera deux pages, en tirera des
conclusions. Le refus explicite coûte un message d'erreur ; le silence coûte des décisions prises sur
un ordre imaginaire. La même logique vaut pour un sens de tri inconnu, qu'on serait tenté de rabattre
sur le croissant.

**Le troisième est le plus subtil et c'est celui qui produit des tickets incompréhensibles.** Trier
par un champ non unique ne définit pas un ordre : deux lignes de même total peuvent sortir dans
n'importe quel ordre relatif, et rien n'oblige le moteur à choisir le même d'une requête à l'autre.
Une pagination construite dessus peut alors montrer deux fois la même ligne, ou n'en montrer aucune,
sans qu'aucune donnée n'ait changé entre les deux appels. Ajouter un départage unique en fin d'ordre
supprime le problème, et c'est exactement la règle démontrée par la leçon de pagination.

Le départage mérite deux précautions. Il est toujours triable, même absent de la liste blanche, parce
qu'il ne vient pas du client mais de la règle de pagination elle-même. Et il n'est ajouté que s'il
n'a pas déjà été demandé : l'ajouter une seconde fois produirait un terme qui ne départage rien,
puisque le premier a déjà tout ordonné.

La liste des champs déjà retenus sert donc deux fois dans le même parcours : à écarter une
répétition, qui ne change rien à l'ordre, et à savoir en sortie de boucle si le départage manque
encore. C'est ce qui permet de tout traiter en une passe.

Le coût est le produit du nombre de termes par la taille de la liste blanche. Les deux se comptent en
unités : la recherche linéaire est ici plus lisible qu'un dictionnaire, et strictement aussi rapide.
