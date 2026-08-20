# Explication

Consommer un message d'une file semble trivial jusqu'a ce qu'on remarque que plusieurs situations
anormales peuvent coexister sur un meme message, et que l'ordre dans lequel on les examine change
la decision. Cet exercice porte precisement sur cette priorite : ce n'est pas seulement quelles
regles on applique, mais dans quel ordre on les consulte.

La premiere regle valide le compteur de livraison. Un nombre de livraisons strictement inferieur a
un n'a aucun sens : un message ne peut pas avoir ete livre moins d'une fois au moment ou on le
traite. C'est une entree invalide, pas une situation metier, et on la refuse par une exception
plutot que de lui inventer une action. Placer cette validation en tete garantit qu'aucune regle
ulterieure ne raisonne sur une valeur absurde.

La deuxieme regle est la mise a l'ecart, et sa position est le coeur pedagogique de l'exercice. Un
message livre au-dela de la limite est un message empoisonne : il a echoue assez de fois pour qu'on
renonce a le retraiter, car chaque nouvel essai risque de rejouer le meme plantage et de bloquer la
file entiere derriere lui. On l'envoie vers une file d'attente morte pour l'isoler et laisser le
flux avancer. Cette regle passe avant la detection de doublon a dessein. Si l'on testait d'abord le
doublon, un message empoisonne qui se trouve aussi etre un identifiant deja vu pourrait etre traite
en boucle au lieu d'etre ecarte ; le cas cache qui combine un compteur de six et un identifiant
present verifie que la mise a l'ecart l'emporte.

La troisieme regle detecte le doublon. Si l'identifiant figure parmi ceux deja traites avec succes,
rejouer l'effet serait une faute : on se contente d'acquitter le message sans agir. La comparaison
se fait par jeton exact contre l'ensemble, jamais par sous-chaine : un identifiant qui serait le
prefixe d'un autre ne doit pas etre confondu avec lui. Construire un ensemble a partir de la liste,
en ignorant les segments vides, donne cette exactitude et un test en temps constant.

La derniere regle est le cas nominal : un message valide, pas encore trop livre, jamais traite, que
l'on traite. Il ferme la cascade et n'est atteint que lorsque aucune anomalie ne s'est presentee.

La lecon generale est celle de la cascade de decisions ordonnee. Quand plusieurs conditions peuvent
etre vraies en meme temps, la correction ne vient pas des conditions isolees mais de leur ordre :
la plus protectrice, ici la mise a l'ecart d'un message empoisonne, doit court-circuiter les autres.
Le cout reste lineaire dans la taille de la liste des identifiants deja traites.
