# Explication

Masquer un secret avant qu'il n'atteigne un journal : la fonction est courte, et chacun de ses
deux choix répond à une attaque précise — c'est un exercice de menace autant que de code.

Premier choix : *aucun* caractère du secret ne survit. La tentation ergonomique est le masque
partiel — garder les quatre derniers caractères « pour identifier la clé » — et l'énoncé
demande justement ce qu'un masque partiel laisserait déduire. Réponse : beaucoup. Les préfixes
de clés identifient le fournisseur et le type de clé ; les suffixes réduisent l'espace de
recherche d'une force brute ; et plusieurs fuites partielles de la même clé se recoupent. Le
masque intégral coupe court — la seule information qui sorte est « il y avait un secret ici ».
Quand l'identification est nécessaire, la réponse n'est pas d'affaiblir le masque mais de
publier un *identifiant* de la clé — un condensat court, une étiquette — géré comme une
donnée non sensible à part entière.

Deuxième choix : la longueur du masque est *plancher* à quatre marqueurs. Un masque qui
reproduit exactement la longueur du secret fuit... la longueur : savoir qu'un mot de passe fait
six caractères oriente une attaque. Le plancher brouille les longueurs courtes — tout secret de
un à quatre caractères produit le même masque — tandis que les longueurs supérieures restent
reflétées, compromis du contrat qui borne la fuite là où elle est la plus dangereuse. Une
version plus stricte masquerait à longueur *fixe* pour tout ; l'exercice retient le plancher,
et la leçon est qu'une politique de masquage doit décider *explicitement* de ce que la forme du
masque révèle.

L'entrée vide rend la chaîne vide — il n'y a rien à masquer, et fabriquer quatre étoiles pour
un champ vide inventerait un secret qui n'existe pas dans la source. La construction
`new string('*', n)` produit le masque en une allocation.

Les cas suivent l'énoncé : la valeur ordinaire masquée à sa longueur, la courte remontée au
plancher, la vide qui reste vide — et les valeurs de test sont elles-mêmes factices, car un
exercice de rédaction de secrets qui embarquerait des vraisemblances de clés réelles se
contredirait.

Le coût est linéaire dans la longueur. La transposition est une règle d'architecture plus
qu'une fonction : le masquage s'applique *à la frontière de sortie* — journaux, messages
d'erreur, réponses de diagnostic — par un composant unique et testé, jamais au cas par cas
dans chaque appel de journalisation. Et son test le plus important est négatif : vérifier
qu'aucun chemin de sortie ne contourne la rédaction — car un seul `ToString` oublié suffit, et
les secrets ne fuient jamais par le chemin qu'on surveillait.
