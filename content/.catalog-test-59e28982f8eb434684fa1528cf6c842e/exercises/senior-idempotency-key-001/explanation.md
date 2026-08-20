# Explication

L'idempotence est la promesse qu'une meme requete, rejouee, ne produit son effet qu'une fois. Le
client ne controle pas le reseau : un accuse de reception perdu le pousse a renvoyer sa demande,
et sans garde-fou le serveur debiterait deux fois le meme compte ou expedierait deux fois la meme
commande. La cle d'idempotence est le jeton qui permet de reconnaitre que deux requetes sont en
verite la meme intention.

L'exercice reduit ce mecanisme a son coeur decisionnel : pour chaque cle lue, faut-il traiter ou
rejouer ? La reponse ne depend que d'une chose, la memoire de ce qu'on a deja vu. La premiere
apparition d'une cle est un traitement veritable ; toute apparition suivante n'est qu'un echo dont
la reponse est deja connue. Un ensemble des cles rencontrees suffit a incarner cette memoire, et
l'operation d'insertion dans un ensemble porte justement l'information voulue : la cle etait-elle
absente ?

L'ordre du test et de l'insertion est le piege central. Si l'on insere la cle avant de verifier sa
presence, l'ensemble la contient toujours au moment du test, et la toute premiere apparition serait
declaree rejeu a tort. Il faut donc interroger la memoire, decider, puis seulement l'enrichir. Ce
detail d'ordonnancement est exactement ce qui distingue un compteur correct d'un compteur decale
d'un cran, et le cas cache d'une cle repetee le met a l'epreuve.

La preservation de l'ordre est la seconde exigence. La sortie doit suivre l'ordre de lecture des
cles, pas un ordre derive de l'ensemble, qui n'en garantit aucun. On construit donc la sortie au
fil du parcours, en ajoutant un verdict par cle, plutot qu'en reparcourant une structure agregee a
la fin. Rendre un unique verdict global trahirait le contrat : le client veut savoir, requete par
requete, laquelle a reellement agi.

Le traitement des segments vides releve de la meme rigueur que toute analyse de liste encodee. Un
separateur en trop, une chaine terminee par un point-virgule, une entree entierement vide : aucun
de ces cas ne doit fabriquer une cle fantome. On les ignore, et une entree vide rend logiquement
une sortie vide, sans cas particulier ajoute a la main. L'entree nulle, elle, n'est pas une liste
vide mais une absence de liste, et on la refuse par une exception plutot que de lui preter un sens.

Le cout est lineaire dans le nombre de cles, chaque cle etant testee et inseree en temps constant
amorti. La leon depasse l'exercice : cote serveur, une cle d'idempotence transforme un reseau non
fiable, ou les messages se dupliquent, en une semantique d'execution unique, a la seule condition
de retenir fidelement ce qui a deja ete vu.
