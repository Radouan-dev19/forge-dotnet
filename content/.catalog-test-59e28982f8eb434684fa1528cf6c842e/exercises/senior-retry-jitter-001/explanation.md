# Explication

Le recul exponentiel répond à la question « quand réessayer » ; le jitter répond à une question que
l'on ne se pose qu'après son premier incident de groupe : « réessayer en même temps que qui ? ». Un
serveur qui tombe fait échouer tous ses clients dans la même seconde. Sans jitter, tous relancent une
seconde plus tard, puis deux, puis quatre — le recul est exponentiel, mais il est **synchronisé**, et
le serveur convalescent reçoit des vagues exactement au moment où il essaie de se relever. C'est le
troupeau tonnant, et il transforme un incident de trente secondes en incident d'une heure.

**Pourquoi le jitter égal plutôt qu'un tirage sur toute la fenêtre.** Tirer l'attente entre zéro et
la fenêtre désynchronise, mais autorise le pire tirage : une attente proche de zéro, c'est-à-dire une
relance quasi immédiate — précisément ce que le recul devait empêcher. La politique du jitter égal
plancher chaque attente à la moitié de sa fenêtre : la moitié basse garantit le repos du serveur, la
moitié haute porte la dispersion. On garde ainsi les deux propriétés — jamais de rafale, jamais de
synchronisation — au prix d'une dispersion moitié moindre, un échange que l'industrie a largement
adopté.

**Pourquoi le déterminisme, alors que le jitter veut de l'aléa.** L'aléa dont le jitter a besoin est
un aléa **entre clients**, pas entre exécutions : deux clients doivent diverger, mais le même client,
rejoué avec la même graine, doit produire le même échéancier — sans quoi aucun test ne peut affirmer
quoi que ce soit sur la politique de relance, et les incidents deviennent irreproductibles. Dériver
le décalage du produit graine fois rang, réduit modulo la moitié plus un, donne exactement cela : la
graine sépare les clients, le rang sépare les attentes d'un même client, et tout reste rejouable. Les
bibliothèques de résilience font le même choix en acceptant une source d'aléa injectable — la
production y branche du vrai hasard, les tests une graine.

**Les deux pièges arithmétiques sont les mêmes qu'en production.** Le produit graine fois rang
déborde l'entier de trente-deux bits dès que la graine est grande — et un débordement dans un modulo
produit des décalages négatifs, donc des attentes sous le plancher, en silence. Le calcul s'élargit
avant de multiplier. Et la fenêtre doublée doit se replafonner à chaque rang : écrêter une fois puis
doubler librement dépasse le plafond dès le rang suivant.

**Les bornes du contrat encodent l'exploitation.** Plus de dix tentatives ne masquent plus un
incident passager mais une panne qu'une alerte doit remonter ; un plafond au-delà de la minute
n'est plus une attente de relance mais un report de traitement, qui relève d'une file. Refuser ces
valeurs au seuil vaut mieux que produire un échéancier plausible pour une politique absurde.

La transposition : clients de bases de données, files de messages, appels de partenaires — partout
où plusieurs instances relancent, l'échéancier se calcule ainsi, et se teste parce qu'il est
déterministe.
