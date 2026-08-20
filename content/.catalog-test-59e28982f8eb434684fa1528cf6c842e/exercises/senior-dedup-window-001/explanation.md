# Explication

La déduplication par fenêtre est le compromis que toutes les messageries finissent par faire : se
souvenir de tout coûte un stockage sans borne, se souvenir un moment coûte un risque. L'exercice
mesure ce risque, et la mesure elle-même enseigne trois choses que les documentations des courtiers
disent rarement en face.

**La fenêtre est un pari sur la distribution des doublons, et le pari perd pendant les incidents.**
En régime normal, les doublons arrivent en secondes — une relivraison sur accusé perdu, une relance
de producteur. La fenêtre d'une heure les attrape tous, et le tableau de bord confirme le choix. Mais
les doublons dangereux ne viennent pas du régime normal : ils viennent des reprises — un courtier
redémarré qui rejoue un segment, une partition réconciliée, une restauration de sauvegarde — et
ceux-là reviennent des heures ou des jours plus tard, précisément quand la fenêtre les a oubliés. Le
comptage de cet exercice rend ce risque visible sur un journal : c'est l'audit qu'on fait après coup,
en se demandant combien la reprise de la nuit a réappliqué.

**La fenêtre glisse, et la livraison échappée rafraîchit quand même la mémoire.** C'est la subtilité
que le troisième exemple force à voir. Le magasin réel ne sait pas qu'une livraison est un doublon
oublié : il traite, puis enregistre ce qu'il vient de traiter. La conséquence est contre-intuitive —
un doublon réappliqué protège les doublons suivants, parce qu'il a remis l'identifiant en mémoire. Un
modèle mental où la mémoire ne se rafraîchit que sur les livraisons « originales » compte faux, et un
audit qui compte faux fait prendre de mauvaises décisions de dimensionnement.

**Le comptage éclaire la vraie décision, qui n'est pas la taille de la fenêtre.** Face à un compte
non nul, l'intuition agrandit la fenêtre. C'est une course perdue : la fenêtre nécessaire est bornée
par la pire reprise possible, qui n'est pas bornée. La réponse durable est ailleurs — rendre le
traitement **idempotent**, pour que le doublon réappliqué ne coûte rien : la déduplication redevient
alors une optimisation de débit au lieu d'être une barrière de correction. C'est l'articulation de
toute la semaine : le registre de rejeu et la clé d'idempotence sont la défense de fond, la fenêtre
n'est que le premier filet.

**Les refus disent ce qu'un audit accepte de mesurer.** Une chronologie qui recule signale un
journal recomposé, dont les écarts n'ont plus de sens ; une fenêtre nulle n'est pas une politique
mais l'absence de déduplication. Dans les deux cas, produire un compte plausible serait pire que
refuser : le chiffre alimenterait une décision de dimensionnement sur des données qui ne mesurent
rien.

En entretien, le terme attendu est fenêtre de déduplication — et la question piège classique est
exactement celle du troisième exemple : « le doublon raté rafraîchit-il la mémoire ? ».
