# Explication

Le paradoxe des suites de tests fragiles tient en une phrase : elles cassent le plus fort au moment où
elles servent le moins. Un remaniement qui préserve le comportement est l'opération pour laquelle on a
écrit les tests — et c'est elle qui en fait tomber quarante d'un coup, non parce que le code est devenu
faux, mais parce que les assertions regardaient au mauvais endroit. Cet exercice donne un critère de
tri, et ce critère mérite d'être compris plutôt que mémorisé.

**La ligne de partage est l'accessibilité, pas l'importance.** Une assertion survit quand ce qu'elle
observe est accessible à un appelant ordinaire : la valeur rendue, l'exception que le contrat promet,
l'état qu'une lecture publique restitue. Elle casse quand elle a besoin d'instruments — un espion qui
compte les appels, une capture d'ordre, une réflexion sur un champ privé, un chronomètre. La nuance
importe : compter les appels à un collaborateur peut sembler essentiel, et c'est parfois le seul moyen
de tester un contrat de non-répétition — mais dans ce cas, la non-répétition **est** le contrat, et
l'assertion devrait porter sur son effet observable, comme un compteur d'envois relisible, pas sur la
mécanique qui l'implémente. Quand l'effet n'est observable nulle part, la question à poser n'est pas
« comment l'espionner » mais « pourquoi le produit ne l'expose-t-il pas ».

**Pourquoi la durée est du mauvais côté.** Une assertion de temps semble observer quelque chose de
réel — l'utilisateur perçoit la lenteur. Mais la durée mesurée dans un test unitaire dépend de la
machine, de la charge, du ramasse-miettes : elle échoue sans changement de code et passe avec une
régression réelle. Un budget de performance se mesure, il ne s'affirme pas dans une suite unitaire ;
la placer dans la famille cassante n'est pas un jugement sur la performance, c'est un jugement sur
l'endroit où on la vérifie.

**Pourquoi une nature inconnue est refusée.** L'inventaire alimente une décision d'équipe : combien de
tests réécrire avant le remaniement. Ignorer une nature non reconnue produirait un inventaire
silencieusement incomplet, du genre qui fait découvrir les vingt derniers tests fragiles au milieu du
remaniement, quand le coût de retour est maximal. Refuser le flux force la mise à jour du
vocabulaire — le désagrément est immédiat, local et bon marché.

**Pourquoi conserver l'ordre et les répétitions.** La sortie sert à retrouver les assertions dans la
suite réelle, où elles ont une position et parfois des doublons légitimes — deux assertions de
résultat sur deux propriétés différentes se déclarent de la même nature. Dédupliquer ou trier
détruirait la correspondance entre l'inventaire et le code qu'il inventorie.

La transposition : avant tout remaniement, compter ce que la suite cimente. Si la mécanique domine,
le premier chantier n'est pas le remaniement — c'est la suite.
