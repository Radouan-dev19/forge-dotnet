# Explication

Compter les groupes de caractères consécutifs identiques — les *plages* — tient en une
observation qui renverse le problème : au lieu de délimiter chaque groupe, il suffit de compter
ses *débuts*. Un groupe commence là où un caractère diffère du précédent, plus une fois au tout
début du texte. Cette reformulation transforme un problème de segmentation en un problème de
comparaison de voisins, et c'est elle que la solution transcrit.

La mécanique en découle : `runs` part de un — le premier caractère ouvre toujours un groupe —
et la boucle démarre à l'indice un, comparant chaque position à la précédente. Un changement
incrémente ; une répétition ne fait rien. Le motif « comparer à son voisin de gauche » évite
toute variable d'état du type « caractère courant du groupe » : l'état est déjà dans le texte,
à l'indice moins un, et le relire coûte moins cher que le recopier. C'est le même squelette que
le calcul d'écarts successifs du bloc tableaux — décalage d'une position entre lecture et
comparaison — avec le même point de vigilance : la boucle sur `i` à partir de un lit `i - 1` en
sécurité par construction.

L'initialisation à un est le siège du hors-par-un local : partir de zéro donnerait un groupe de
moins sur *tous* les textes non vides — l'erreur uniforme, plausible en lecture rapide, que le
cas d'un caractère unique expose : la réponse est un, pas zéro. Mais partir de un exige alors
que le texte vide soit traité *avant*, d'où la garde initiale qui rend zéro : zéro caractère,
zéro groupe. L'ordre garde-puis-initialisation n'est pas interchangeable, et c'est un bon
exemple d'un couple de décisions qui ne se comprennent qu'ensemble.

Les cas cachés balaient les extrêmes de la structure : le texte uniforme — un seul groupe,
quelle que soit sa longueur —, le texte alterné — autant de groupes que de caractères —, le
caractère unique, et le vide. Ces quatre-là encadrent toutes les implémentations fautives
usuelles ; le nominal `aaabb` de l'énoncé, avec ses deux groupes, réfute le comptage de
caractères distincts — `aabaa` a trois groupes mais deux caractères distincts, et un caché de
cette forme sépare définitivement les deux lectures du problème.

Le coût est linéaire, l'espace constant, aucune allocation — la version qui matérialise les
groupes pour les compter ferait le même verdict au prix de segments inutiles.

La transposition est le début de la compression par plages — l'étape de comptage de l'encodage
RLE — et, plus largement, toute détection de *transitions* dans une séquence : changements
d'état dans un journal, alternances de statut, sessions découpées par inactivité. Compter les
débuts plutôt que les contenus : la reformulation vaut pour toutes ces variantes.
