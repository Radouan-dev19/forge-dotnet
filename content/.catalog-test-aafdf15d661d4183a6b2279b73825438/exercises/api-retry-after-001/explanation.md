# Explication

Le `Retry-After` est la moitié honnête d'une limite de débit : dire non ne suffit pas, il faut
dire *quand réessayer*, sinon le client refusé retente immédiatement et en boucle, ajoutant de la
charge au service déjà saturé. Cet exercice calcule ce délai pour une fenêtre fixe, et sa valeur
tient dans deux décisions ordonnées et une borne.

La première décision est : le client est-il seulement limité ? Sous le quota, il ne l'est pas, et
la réponse est zéro — il n'a rien à attendre, il peut appeler tout de suite. Renvoyer un délai
non nul à un client sous son quota est l'erreur qui ralentit inutilement un usage parfaitement
légitime. Ce n'est qu'*au* quota, ou au-delà, que la question du délai se pose. L'ordre compte :
on décide d'abord de la limitation, on calcule ensuite le délai.

La seconde décision est l'instant cible. Dans une fenêtre fixe, le compteur se réinitialise au
*début* de la tranche suivante — l'instant `windowStartUnix + windowSeconds` — et c'est jusque-là
que le client doit patienter. L'erreur fréquente vise la *fin* de la fenêtre courante, qui est le
même instant par définition, mais le raisonnement se trompe souvent en calculant depuis la fin de
la fenêtre *précédente* ou en ajoutant une durée de trop. Le repère sûr est « début de la
fenêtre suivante », et le délai est l'écart entre cet instant et maintenant. C'est aussi ce qui
révèle le défaut de bord de la fenêtre fixe, vu en leçon : deux clients au quota, l'un en début
et l'autre en fin de tranche, reçoivent des délais très différents, et le second pourra rafaler à
la réinitialisation — le prix de la simplicité de cette stratégie.

La borne à zéro par le bas ferme le dernier cas : si l'horloge a déjà dépassé la réinitialisation
— la fenêtre s'est écoulée sans que l'état soit rafraîchi — le délai calculé serait négatif, et
un `Retry-After` négatif n'a aucun sens. Le ramener à zéro dit « tu peux réessayer maintenant »,
ce qui est correct : la fenêtre est passée. Le cas caché de la fenêtre dépassée verrouille cette
borne.

L'arithmétique en 64 bits est la précaution habituelle des calculs d'instants : la somme d'un
horodatage et d'une durée, ou leur différence, peut sortir du domaine d'un entier de 32 bits, et
un délai enroulé donnerait un `Retry-After` aberrant.

Le coût est constant. La transposition est le principe de la limite honnête : chaque refus
temporaire — débit, verrou occupé, ressource indisponible — gagne à dire quand réessayer, et le
calcul de ce délai suit toujours le même squelette : suis-je bloqué, jusqu'à quel instant précis,
et ce délai est-il borné à zéro. Un service qui refuse sans dire quand transforme ses clients en
marteaux-piqueurs.
