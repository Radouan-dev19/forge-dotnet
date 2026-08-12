# Explication

Deux parties expriment une préférence sur le même choix, et il faut décider laquelle commande.

Le client sait ce qu'il peut lire ; c'est donc lui qui classe, par son facteur de qualité. Le serveur
sait ce qu'il produit le mieux ; il n'intervient que pour départager deux types que le client place
au même niveau. Inverser cet ordre paraît anodin et ne l'est pas : la réponse dépendrait alors de
l'ordre dans lequel le client a écrit sa liste, et deux clients équivalents recevraient des
représentations différentes.

La qualité nulle est le piège du sujet. Elle ressemble à une absence de préférence et c'en est
l'exact contraire : le client dit qu'il ne veut **pas** ce type. Une implémentation qui traite zéro
comme « rien de précisé » retombera sur le passe-partout et enverra précisément ce qui a été refusé.
D'où la règle de recherche : une entrée exacte gagne toujours sur le passe-partout, y compris quand
elle vaut zéro.

Le parcours suit les types du serveur plutôt que ceux du client, et la comparaison est strictement
supérieure. Ces deux détails ensemble suffisent à faire respecter le départage : le premier type
rencontré à la meilleure qualité est aussi le plus haut dans la préférence du serveur, et rien
d'égal ne viendra le déloger.

Aucune correspondance rend une chaîne vide plutôt qu'un refus. La fonction décide d'une
représentation ; c'est à la couche HTTP de transformer cette absence en réponse. Mélanger les deux
rendrait la règle inutilisable ailleurs — dans un test, dans un autre transport, dans un journal.

Le coût est le produit des deux listes, ce qui reste négligeable : ces listes comptent quelques
entrées, jamais des milliers.
