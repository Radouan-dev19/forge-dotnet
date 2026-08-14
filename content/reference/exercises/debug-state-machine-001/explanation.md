# Explication

Rejouer une séquence d'événements pour connaître l'état final : c'est une machine à états dans
sa forme la plus nue, et l'exercice appartient au débogage parce que rejouer les événements
*est* la technique reine pour comprendre comment un système est arrivé dans un état absurde —
on reprend le journal, on applique les transitions, on regarde où ça diverge.

La machine se lit dans les gardes : trois états — initial, démarré, achevé — et trois
événements dont chacun n'agit que depuis certains états. Le démarrage n'opère que depuis
l'initial, l'achèvement que depuis le démarré, la réinitialisation depuis partout. Chaque
condition est donc une *paire* événement-et-état — `value == 1 && state == 0` — et c'est le
point que le titre souligne : les transitions sont *bornées*. Une implémentation qui réagit à
l'événement seul — voir un deux, passer à deux — accepte des trajectoires interdites :
l'achèvement sans démarrage, le démarrage double. Le cas caché qui envoie l'achèvement en
premier départage les deux lectures : la machine correcte l'ignore et finit à zéro.

L'autre moitié du contrat est le sort des événements *hors état* : ils s'ignorent,
silencieusement — pas d'exception, pas d'état d'erreur. C'est un choix de tolérance qui se
défend pour un rejou de journal — les doublons et les messages en retard sont le quotidien des
flux d'événements, et une machine qui s'effondre au premier doublon ne dépouille rien — et il a
son alternative stricte, où toute transition illégale est une faute détectée. Les deux
existent en production ; la version tolérante *masque* les anomalies qu'elle absorbe, et un
système réel lui adjoindrait au moins un compteur d'événements ignorés. Savoir lequel des deux
régimes on écrit, et le dire, est la moitié du travail.

La structure if-else en chaîne convient à trois états ; la version table — un dictionnaire de
paire état-événement vers état — devient préférable dès que la machine grossit, et l'expression
`switch` sur tuple `(state, value)` est l'entre-deux idiomatique moderne. Le choix est
d'échelle, pas de principe : ce qui compte est que *toutes* les transitions légales soient
énumérées quelque part, et que l'illégal ait un sort décidé.

Les cas cachés jouent les trajectoires : le parcours nominal complet, la réinitialisation en
plein milieu suivie d'un redémarrage, les événements ignorés en rafale, et la séquence vide —
état initial, la boucle ne tourne pas.

Le coût est linéaire, l'état tient dans un entier. La transposition est double : côté
conception, tout cycle de vie — commande, ticket, session — est une machine dont les
transitions doivent être bornées par paires ; côté diagnostic, quand un objet est dans un état
impossible, la question n'est pas « où est le bug ? » mais « quelle séquence d'événements
rejouée mène ici ? » — et cette fonction-là est l'outil qu'on écrit pour y répondre.
