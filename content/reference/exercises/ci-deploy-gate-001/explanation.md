# Explication

Trois conditions, une conjonction, et le déploiement part ou ne part pas. La fonction est le
noyau décidable d'une *porte de déploiement*, et chaque terme mérite sa défense — ainsi que la
question rituelle de l'énoncé : que vérifie une porte qui n'a jamais bloqué personne ?

Les termes d'abord. Les preuves vertes — la suite de tests passée sur *l'artefact candidat* —
sont la condition évidente ; son piège est ailleurs, dans la tentation du passage en force
quand « c'est urgent et les tests sont cassés pour une autre raison ». L'environnement protégé
dit que la cible exige la porte : les environnements de production se déclarent protégés dans
la chaîne, et un déploiement vers une cible non déclarée échoue par défaut — la protection est
une propriété de la *cible*, pas une politesse du déployeur. L'approbation enfin — un humain
nommé a regardé et consenti : elle n'ajoute aucune vérification technique, elle ajoute une
*responsabilité* — quelqu'un savait, quelqu'un répond — et une fenêtre : le déploiement part
quand l'équipe est prête à l'observer, pas quand la chaîne finit.

La conjonction stricte est le contrat : deux conditions sur trois, c'est une porte ouverte —
des tests verts déployés sans approbation vers une cible non protégée, ou l'inverse. Les cas
de l'énoncé le déroulent : la porte ouverte, puis chaque condition retirée isolément — chaque
terme prouvé nécessaire, le domaine booléen couvert.

La question rituelle maintenant, et sa réponse honnête : une porte qui n'a jamais bloqué ne
vérifie *rien* — elle enregistre. Si l'approbation est donnée par réflexe, si les tests rouges
se contournent par relance jusqu'au vert, la porte est un tampon, et le tampon donne l'illusion
du contrôle — plus dangereuse que l'absence de contrôle, car elle endort. Le critère de santé
d'une porte est qu'elle bloque *parfois*, et que chaque blocage déclenche une conversation :
c'est la preuve qu'elle est sur le chemin des vraies décisions. Le pendant culturel : une
porte qui bloque *toujours* pousse aux contournements — le calibrage est un travail d'équipe
continu.

Cette fonction pure est la *règle* ; la chaîne réelle du laboratoire de livraison du parcours
câble sa mise en œuvre — l'environnement protégé, la dépendance aux travaux de test — et les
deux se complètent : la règle se teste par table, le câblage se lit dans le fichier de
définition.

Le coût est constant. La transposition : toute action irréversible d'un système — publication,
migration, suppression — mérite sa porte à conjonction explicite, dont chaque terme se justifie
en une phrase et dont les blocages sont des événements normaux, tracés et discutés. Une porte
se juge à ses refus.
