# Le cloud en un modèle mental

Une facture cloud paie toujours l'une de ces trois choses : du matériel loué à l'heure, une
plateforme qui exécute votre code sans que vous gériez le système d'exploitation dessous, ou
une fonction qui ne facture que le temps réellement exécuté. Confondre les trois, c'est
payer pour un niveau de service qu'on n'utilise pas ou, pire, croire qu'on a une garantie
qu'on n'a pas achetée.

## Les trois niveaux, par ce qu'ils vous laissent gérer

**IaaS** (Infrastructure as a Service) loue une machine virtuelle : vous choisissez l'OS,
vous l'installez, vous le patchez, vous gérez les pannes disque. C'est le niveau le plus
proche d'un serveur physique — le plus de contrôle, le plus de responsabilité.

**PaaS** (Platform as a Service, ex. Azure App Service) prend le système d'exploitation et
le runtime à sa charge : vous déployez votre application, la plateforme gère les mises à
jour de sécurité, le redémarrage après crash, la mise à l'échelle horizontale. Vous perdez
l'accès root ; vous gagnez de ne plus jamais patcher un noyau un dimanche soir.

**Serverless/FaaS** (Functions as a Service) pousse le curseur au bout : pas de serveur à
dimensionner du tout, une fonction qui démarre à la sollicitation et facture au temps
d'exécution, à la milliseconde près. La contrepartie est le *cold start* — la première
invocation après une période d'inactivité est plus lente — et une durée d'exécution
plafonnée.

Aucun niveau n'est « meilleur » : un batch qui tourne en continu coûte moins cher en IaaS
qu'en serverless ; une API interne à trafic irrégulier coûte souvent moins cher en
serverless qu'en VM qui tourne 24h/24 pour traiter deux requêtes par heure.

## Région et zone de disponibilité ne sont pas un détail administratif

Une **région** est un ensemble de centres de données dans une zone géographique (France
Centre, West Europe…) : elle fixe la latence pour vos utilisateurs et, souvent, la
conformité réglementaire des données qui y résident. Une **zone de disponibilité** est un
centre de données physiquement isolé à l'intérieur d'une région, avec sa propre alimentation
et son propre réseau. Déployer sur une seule zone, c'est accepter qu'un incendie ou une
coupure électrique localisée arrête tout ; répartir sur plusieurs zones d'une même région,
c'est survivre à cet incident sans changer de région ni renoncer à la faible latence entre
zones.

## Élasticité et résilience : deux problèmes différents

L'**élasticité** répond à « combien de charge puis-je absorber sans intervention manuelle » :
ajouter des instances quand le trafic monte, en retirer quand il redescend, pour payer ce
qu'on consomme plutôt qu'un pic dimensionné en permanence. La **résilience** répond à
« qu'est-ce qui continue de fonctionner quand un composant tombe » : une seule instance,
même auto-scalée, reste un point de panne unique tant qu'elle est seule au moment de
l'incident. Un système correctement conçu traite les deux séparément — la panne d'une
instance ne doit jamais dépendre de la charge du moment pour être absorbée.

## Le compromis qui ne disparaît jamais

Chaque niveau d'abstraction du cloud échange du contrôle contre de la responsabilité
opérationnelle en moins. Le fournisseur ne prend jamais en charge la conception de votre
système : la répartition en zones, le choix du bon niveau de service, la stratégie de reprise
après incident restent de votre ressort quel que soit le niveau choisi. Le cloud déplace
l'effort, il ne le supprime pas.

## Ce qu'il faut retenir

IaaS, PaaS et serverless ne sont pas trois marques du même produit : ce sont trois contrats
différents sur qui gère quoi. Une région choisit où vivent vos données et avec quelle
latence ; une zone de disponibilité choisit si un incident local vous arrête. Et l'élasticité
ne remplace jamais la résilience — l'une absorbe la charge, l'autre absorbe la panne.
