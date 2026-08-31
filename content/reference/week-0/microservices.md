# Microservices : les concepts pour en parler

Une architecture microservices découpe une application en services indépendants, chacun
responsable d'une capacité métier — commandes, facturation, catalogue — et déployable seul. Le mot
est à la mode ; en entretien, ce qui compte n'est pas de le prononcer mais de savoir dire ce que ce
découpage achète, ce qu'il coûte, et quand il ne se justifie pas.

## Le point de départ : ce que le monolithe fait bien, et où il coince

Un monolithe — une seule application déployée d'un bloc — n'est pas une faute : il est simple à
développer, à tester et à déployer tant que l'équipe et le domaine restent petits. Il coince quand
plusieurs équipes se marchent dessus dans la même base de code, quand une modification mineure
exige de redéployer et retester l'ensemble, ou quand une seule partie du système a besoin de monter
en charge alors que tout le reste suit. Les microservices répondent à ces trois douleurs précises —
pas à une envie de modernité.

## Découplage : la frontière passe par le métier, pas par la technique

Un service se découpe autour d'une **capacité métier** et de son vocabulaire — le « contexte
borné » du domaine — jamais autour d'une couche technique. Un service « commandes » qui possède
tout ce qui concerne une commande est une frontière saine ; un service « accès aux données »
partagé par tous est un monolithe distribué : les mêmes dépendances qu'avant, plus le réseau au
milieu. Le test d'une bonne frontière : une évolution métier courante ne doit toucher qu'un seul
service.

## Chaque service possède ses données

La règle qui fait le plus mal et qui compte le plus : **pas de base de données partagée**. Chaque
service possède son schéma, et personne d'autre n'y écrit ni n'y lit directement. C'est ce qui rend
l'indépendance réelle — un service peut changer son modèle sans casser les autres. Le prix : plus
de jointure SQL entre deux services ; les données se rapprochent par appel d'API ou par duplication
contrôlée, et la cohérence globale devient **éventuelle** : après une commande, le service de
facturation apprend la nouvelle un peu plus tard, et le système converge au lieu d'être exact à
chaque instant.

## Communiquer : API synchrone ou bus asynchrone

Deux canaux, deux usages. L'appel **synchrone** (HTTP/REST, gRPC) sert quand l'appelant a besoin de
la réponse maintenant — vérifier un stock avant de confirmer. Il est simple mais couple la
disponibilité : si le service appelé est lent ou tombé, l'appelant l'est aussi, et une chaîne
d'appels synchrones cumule les pannes. Le canal **asynchrone** passe par un courtier de messages —
Azure Service Bus, RabbitMQ — : le producteur publie un événement (« commande créée ») et repart ;
les consommateurs le traitent à leur rythme. Cela absorbe les pics, tolère l'indisponibilité
momentanée d'un consommateur et permet d'ajouter un abonné sans toucher au producteur. Le prix : la
livraison peut arriver en double ou en retard, donc les traitements doivent être **idempotents** —
rejouer le même message ne doit rien changer de plus.

## Indépendance de déploiement : le vrai critère

La promesse centrale se vérifie d'une phrase : *peut-on livrer ce service un mardi après-midi sans
coordonner personne d'autre ?* Cela suppose des contrats d'API stables et versionnés — on ajoute,
on ne casse pas —, un pipeline de livraison par service, et une supervision par service. Si chaque
mise en production exige de livrer trois services ensemble dans le bon ordre, le découpage est
raté : on a les coûts des microservices sans leur bénéfice.

## Ce que ça coûte, et quand ne pas le faire

Le réseau devient omniprésent : latence, pannes partielles, retries, disjoncteurs, traçage d'une
requête à travers cinq services — autant de complexité qui n'existait pas dans le monolithe. La
réponse honnête en entretien : une petite équipe sur un domaine encore flou vit mieux avec un
**monolithe modulaire** — des frontières nettes à l'intérieur d'un seul déployable — et n'extrait
un service que lorsqu'une frontière a fait ses preuves et qu'une douleur réelle (équipes, charge,
cadence de livraison) le justifie. Extraire trop tôt fige un mauvais découpage derrière des
contrats d'API.

## Le relier à une migration réelle

Une migration d'un ERP WebForms monolithique vers .NET 8 illustre exactement ce raisonnement : on
ne réécrit pas tout d'un coup, on isole d'abord des modules aux frontières nettes — promotions,
facturation — derrière des interfaces, on les teste et on les livre séparément, et chaque module
ainsi dégagé est un candidat naturel à devenir un service si le besoin de déploiement indépendant
se confirme. C'est le motif dit de l'étrangleur : la nouvelle architecture pousse autour de
l'ancienne, module par module, sans arrêt du système. Présenté ainsi, un projet de migration
devient une histoire d'architecture, pas seulement un changement de framework.

## Ce qu'il faut retenir

Les microservices achètent l'indépendance — d'équipe, de déploiement, de montée en charge — au prix
de la complexité distribuée : cohérence éventuelle, idempotence, supervision par service. La
frontière suit le métier et chaque service possède ses données. Synchrone quand on attend la
réponse, asynchrone par bus pour découpler et absorber. Et la meilleure réponse d'entretien reste
souvent : « pas de microservices sans douleur qui les justifie — un monolithe modulaire d'abord,
l'extraction ensuite ».
