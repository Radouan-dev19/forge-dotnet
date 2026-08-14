# Explication

Compter les mesures dont l'amplitude dépasse un seuil : un prédicat de plus dans la famille des
comptages, et pourtant cet exercice appartient au domaine du débogage — parce que son prédicat
contient le piège d'exécution le plus asymétrique du langage, celui qui ne se déclenche que sur
*une seule valeur* parmi quatre milliards.

Le voici : `Math.Abs(int.MinValue)` lève `OverflowException`. La plage des entiers signés est
asymétrique — le minimum n'a pas d'opposé représentable, deux milliards cent millions de négatifs contre
un de moins côté positif — et la valeur absolue du minimum n'existe donc pas dans
le type. Un comptage d'anomalies écrit naïvement fonctionne des années, puis un capteur en
panne envoie la valeur sentinelle du pire cas, et le tableau de bord tombe — non pas sur une
donnée étrange, mais sur *la* donnée que le type ne peut pas retourner. La parade de la
solution tient dans le transtypage *avant* l'appel : `(long)value` élargit d'abord, et l'opposé
de n'importe quel `int` tient confortablement en `long`. La comparaison au seuil se fait ensuite
entre `long` et `int`, promue sans perte.

L'ordre des opérations est ce qui compte : élargir puis prendre la valeur absolue. L'écriture
inverse — `(long)Math.Abs(value)` — lève toujours, l'exception naissant dans l'appel intérieur
avant que le transtypage n'existe. C'est un cas d'école de correction qui *semble* équivalente
et ne l'est pas, exactement le genre de nuance qu'une revue attrape quand elle sait où
regarder — et qu'un cas de test posé sur `int.MinValue` fige pour toujours, ce que les cachés
font ici.

Le reste du prédicat est de la précision de contrat : le seuil est *strictement* dépassé —
`>` et non `>=` — si bien qu'une mesure posée exactement sur le seuil n'est pas une anomalie ;
le cas caché à égalité départage. L'amplitude se mesure des deux côtés — moins dix dépasse huit
autant que dix — et c'est la raison d'être de la valeur absolue dans le prédicat. Le tableau
vide rend zéro, aucun cas spécial.

Le coût est linéaire, sans allocation. La transposition est double, comme souvent dans cette
famille. Côté domaine : tout seuil d'alerte sur des mesures signées — écarts de température,
variations de solde, dérives d'horloge — porte la même paire de questions, stricte ou large, et
amplitude ou valeur signée. Côté langage : chaque usage de `Math.Abs`, de la négation unaire ou
de `checked` sur des entiers signés mérite une seconde de réflexion sur la valeur minimale — la
seule qui n'a pas de miroir, et celle que les données réelles finissent toujours par contenir.
