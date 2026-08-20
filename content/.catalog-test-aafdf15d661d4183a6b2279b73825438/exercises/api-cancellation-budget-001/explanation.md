# Explication

Un minimum de deux entiers : le calcul est dérisoire, et la règle qu'il encode gouverne la survie
des services sous charge. Cet exercice extrait le noyau décidable d'une politique d'annulation —
qui, du client ou du serveur, décide du temps qu'une requête a le droit de consommer ?

La réponse du contrat : les deux, et c'est le plus contraignant qui gagne. Le client peut
demander moins que le plafond — il sait que sa page abandonne au bout de cinq secondes, inutile
que le serveur travaille trente — et sa demande est honorée. Il peut demander plus — par bug ou
par gourmandise — et le plafond du serveur le rabat. `Math.Min` est la traduction exacte : une
négociation où chacun apporte sa borne et où la plus stricte l'emporte. L'énoncé demande de
nommer ce qu'un budget non borné laisserait consommer, et la réponse mérite d'être écrite : un
fil de traitement, une connexion, de la mémoire — retenus aussi longtemps que le client le
demande, par *tous* les clients à la fois. Le plafond serveur n'est pas une politesse, c'est la
digue qui empêche un client lent — ou hostile — de retenir les ressources de tous les autres.

La validation refuse le nul et le négatif des deux côtés, et le nul est le cas instructif : un
délai de zéro seconde n'est pas « pas de limite », c'est une requête déjà expirée — et dans les
API de délais réelles, zéro ou l'infini sont des conventions qui varient d'une bibliothèque à
l'autre, source inépuisable de confusion. Le contrat de l'exercice coupe court : les durées
sont strictement positives, tout le reste est une faute d'appel. Trancher les conventions
ambiguës à la frontière plutôt que les interpréter au fond : c'est un principe qui économise
des heures de spéculation.

L'égalité des deux bornes rend leur valeur commune — le cas caché posé dessus vérifie qu'aucune
des deux comparaisons strictes n'écarte ce cas — et l'ordre des paramètres n'influence rien,
le minimum étant commutatif : deux propriétés triviales à tester, et qui figent l'intention.

Le coût est constant. La transposition est le motif de la *négociation de ressources* entier :
taille de page demandée contre plafond de pagination, durée de session demandée contre maximum
de sécurité, taille de téléversement contre limite du serveur. Chaque fois, la même
structure — deux volontés, une règle de composition, la plus stricte gagne — et le même piège à
éviter : faire confiance à la borne d'en face. Le serveur qui applique la demande du client
sans la borner a déjà donné les clés de sa file d'attente.
