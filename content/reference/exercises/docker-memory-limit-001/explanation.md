# Explication

Borner la mémoire d'un conteneur : un défaut pour l'absence de demande, un plancher, un
plafond. La mécanique combine deux motifs connus — le défaut pour l'invalide, la double borne
pour l'explicite — et la valeur de l'exercice est dans la question de l'énoncé : que produit
une limite trop basse, et que produit une limite absente ?

La limite absente d'abord : un conteneur sans plafond mémoire peut consommer celle de l'hôte
entier, et quand la machine suffoque, c'est le tueur de processus du noyau qui choisit les
victimes — souvent pas le coupable. Un voisin de machine mal borné devient ainsi la cause des
pannes de tous les autres : c'est le problème du voisin bruyant, et la limite par conteneur
est la cloison qui l'endigue. L'absence de limite n'est jamais une politique, c'est une
absence de politique.

La limite trop basse ensuite : le conteneur qui atteint son plafond est tué par l'hôte — arrêt
brutal, sans grâce, au milieu de ce qu'il faisait — puis redémarré, jusqu'au prochain plafond.
Une limite sous le besoin réel du service fabrique donc une boucle de crash mémoire, cousine
de la boucle de redémarrage des sondes trop pressées. Le *plancher* de la fonction — cent
vingt-huit mégaoctets — encode cette leçon : une demande en dessous n'est pas une économie,
c'est une panne programmée, et la politique la remonte au minimum viable.

La structure de la fonction distingue les deux régimes d'entrée, comme la taille de page
voisine : la demande *non positive* — absente, zéro, négative — reçoit le défaut de deux cent
cinquante-six, la valeur d'équipe raisonnable ; la demande *explicite* est respectée mais
bornée des deux côtés par `Math.Clamp` — remontée au plancher, rabattue au plafond de mille
vingt-quatre. Le plafond protège l'hôte, le plancher protège le conteneur, le défaut protège
contre l'oubli : trois protections, trois constantes nommées dans le contrat.

Les cas de l'énoncé couvrent la grille : la valeur intérieure inchangée, les deux bornes
exactes qui passent telles quelles — incluses —, le dessous remonté, le dessus rabattu, le nul
vers le défaut. Le triplet de frontière habituel, appliqué deux fois.

Le coût est constant. La transposition vaut pour toutes les ressources contraintes —
processeur, descripteurs, connexions, quotas de requêtes : chaque consommateur déclare sa
demande, la plateforme applique défaut-plancher-plafond, et les trois constantes sont des
décisions d'équipe écrites, ajustées à la mesure — jamais des nombres magiques dispersés. Une
politique de ressources tient en trois nombres par ressource ; son absence tient l'astreinte
éveillée.
