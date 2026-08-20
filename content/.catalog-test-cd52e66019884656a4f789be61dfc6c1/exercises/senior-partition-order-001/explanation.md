# Explication

L'ordre des messages est la garantie la plus mal comprise des journaux partitionnés, parce qu'elle
est vraie et fausse à la fois : vraie dans une partition, fausse partout ailleurs. Les équipes qui
découvrent la nuance en production la découvrent par son symptôme le plus déroutant — une entité dont
les états arrivent dans le désordre, sans qu'aucun message ne manque ni ne soit corrompu — et le
diagnostic échoue tant qu'on cherche au mauvais endroit.

**Pourquoi le désordre naît dans le routage et pas dans le transport.** Deux messages de la même clé
sur la même partition sont consommés dans l'ordre d'écriture : c'est le contrat du journal. Éclatés
sur deux partitions, ils sont consommés par deux fils indépendants, à des rythmes indépendants — et
« expédiée » peut arriver après « annulée » sans qu'aucun composant ait mal fonctionné. Les
horodatages n'y peuvent rien : ils datent l'émission, pas la consommation, et les comparer entre
partitions revient à comparer des horloges qui ne se sont rien promis. Le seul endroit où la dérive se
voit est le journal de routage lui-même : une clé, plusieurs partitions.

**Pourquoi les causes sont presque toujours des changements innocents.** L'affectation clé-partition
passe par une fonction de hachage, et tout ce qui touche à ses entrées la change : un
repartitionnement qui modifie le nombre de partitions, un correctif qui normalise la casse de la clé,
un producteur reconfiguré qui route à la ronde « pour équilibrer ». Chacun de ces changements est
raisonnable isolément et casse l'ordre pour les entités dont les messages chevauchent le changement.
C'est pourquoi l'audit se rejoue après chaque déploiement côté producteur — la dérive est un
événement, pas un état permanent.

**Pourquoi le verdict est binaire par clé.** Une clé sur deux partitions a perdu la garantie autant
qu'une clé sur dix : le consommateur doit soit tolérer le désordre — version, horloge logique — soit
réparer le routage. Compter les partitions visitées n'ajouterait rien à la décision ; en revanche, la
référence à la **première** partition vue, plutôt qu'à la précédente, rend le verdict indépendant du
fragment de journal reçu — la même robustesse que tout audit qui se rejoue sur des extraits.

**La question finale de l'énoncé est le vrai arbitrage.** Tout router vers une partition unique
préserve tous les ordres et détruit tout le parallélisme : le journal redevient une file. Le
partitionnement par clé est précisément le compromis — l'ordre là où il compte, entité par entité, le
parallélisme entre entités — et le préserver exige une seule discipline : la clé de routage est un
contrat, pas un détail d'implémentation.

En entretien, ce sujet arrive avec le vocabulaire des courtiers à journal — partition, clé de
partitionnement, garantie d'ordre — et la bonne réponse tient en une phrase : l'ordre est par
partition, donc par clé, donc la clé se choisit comme on choisit une frontière.
