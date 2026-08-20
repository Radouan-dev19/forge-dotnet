# Explication

Le temps de résorption d'une file est le calcul d'astreinte par excellence : trois nombres, une
soustraction, une division — et pourtant il se fait faux dans la panique, avec des conséquences
chères. L'exercice le fige, et chacune de ses trois branches corrige une erreur observée en incident.

**L'erreur centrale : diviser par la consommation brute.** L'intuition prend l'arriéré et le divise
par ce que les consommateurs traitent par minute. Mais pendant le drainage, les producteurs
continuent : ce qui résorbe l'arriéré n'est pas la consommation, c'est le **débit net** — la
consommation moins les arrivées. Une file de six cents messages avec cent consommations et quarante
arrivées par minute ne se vide pas en six minutes mais en dix ; et l'écart grandit à mesure que les
débits se rapprochent. C'est aussi ce qui explique la déception classique de l'astreinte : doubler
les consommateurs ne double pas le débit net — passer de cent à deux cents consommations avec
quatre-vingt-dix arrivées fait passer le net de dix à cent dix, un facteur onze, alors que passer de
cent à deux cents avec dix arrivées ne gagne « que » un facteur un virgule un — l'effet du doublement
dépend entièrement de la proximité des deux débits, et seul le calcul du net le montre.

**L'impossibilité est un verdict, pas une exception.** Quand la consommation n'excède pas les
arrivées, aucune durée finie n'existe : la file stagne ou grossit. Lever une exception serait
confondre « la réponse est qu'il n'y en a pas » avec « la question est mal posée » — or les trois
nombres décrivent un état parfaitement réel, celui qui exige une action de fond : plus de
consommateurs, moins de producteurs, ou un délestage. Le moins un est la réponse qui déclenche cette
action ; l'exception l'aurait cachée dans un journal d'erreurs.

**L'arrondi va vers le haut, et le débordement guette la formule.** Une minute entamée est une minute
d'attente : annoncer neuf minutes quand la dixième est entamée fait rater le message « c'est
résorbé » d'une minute — anodin pour un humain, faux pour l'automate qui lève l'alerte à
l'échéance. Le quotient plafond en entiers, arriéré plus net moins un sur net, a sa chausse-trape :
la somme intermédiaire dépasse l'entier de trente-deux bits quand l'arriéré est immense et le net
minuscule — précisément le cas des grandes pannes. Le calcul s'élargit avant d'additionner.

**L'arriéré nul répond zéro, même si la file va grossir.** La fonction répond à la question posée —
combien de temps pour résorber **cet** arriéré — et zéro est la réponse exacte. Le débit net
défavorable est un autre signal, qui mérite sa propre alerte ; les fusionner rendrait les deux
illisibles.

En entretien, ce raisonnement s'adosse au vocabulaire des files — backlog, débit, consumer lag — et
la question type est exactement celle de l'énoncé : « on double les consommateurs, que se
passe-t-il ? ».
