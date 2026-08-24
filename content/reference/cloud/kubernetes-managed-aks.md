# Kubernetes managé : ce qu'Azure Kubernetes Service retire de la charge

Installer et faire vivre un cluster Kubernetes soi-même est un métier à part entière : le
plan de contrôle (l'ensemble des composants qui décident où placer les Pods, stockent l'état
désiré, exposent l'API du cluster) doit être disponible en permanence, mis à jour, sauvegardé,
sécurisé. Azure Kubernetes Service (AKS) prend ce plan de contrôle à sa charge — c'est le
même déplacement de responsabilité que d'IaaS vers PaaS, appliqué à Kubernetes.

## Ce qu'AKS gère à votre place

Le plan de contrôle est géré et hautement disponible sans que vous ayez à provisionner ni
surveiller les machines qui l'exécutent — et sans en payer directement le coût de calcul, à
la différence des nœuds de travail. Les mises à jour de version de Kubernetes sur le plan de
contrôle se déclenchent en quelques clics plutôt que par une procédure de migration manuelle
risquée. L'intégration native avec Azure Active Directory, Azure Monitor et Azure Container
Registry élimine la configuration que ces mêmes intégrations demanderaient sur un cluster
auto-hébergé.

## Ce qui reste de votre ressort

Les **nœuds de travail** — les machines virtuelles qui exécutent réellement vos Pods — restent
à dimensionner, et vous payez leur temps de calcul comme n'importe quelle VM. La définition
de vos Deployments, Services et politiques réseau reste entièrement la vôtre : AKS gère le
cluster, pas ce que vous y déployez. Et la même discipline de sécurité s'applique — identité
managée pour accéder à Key Vault ou à une base de données, images de conteneur scannées avant
déploiement — un cluster managé ne dispense d'aucune des bonnes pratiques développées ailleurs
dans ce chapitre.

## Quand un cluster Kubernetes est justifié

Un cluster devient pertinent quand plusieurs signes se cumulent : plusieurs équipes déploient
des dizaines de services indépendants qui doivent partager de l'infrastructure réseau et de
l'observabilité communes ; les besoins en orchestration dépassent ce qu'App Service ou
Container Apps exposent (placement fin sur du matériel spécifique, contrôleurs personnalisés,
patrons multi-conteneurs par Pod) ; l'équipe a déjà l'expertise Kubernetes ou le budget pour
l'acquérir, parce que même managé, un cluster demande une compréhension réelle de ses
concepts pour être exploité sereinement.

## Quand il ne l'est pas

Une seule application, une seule équipe, un besoin de scale-to-zero ou un trafic irrégulier :
Container Apps offre l'essentiel des bénéfices de l'orchestration (mise à l'échelle
automatique, révisions, healthchecks) sans la surface de configuration d'un cluster complet.
Choisir Kubernetes pour un seul service parce que c'est la solution la plus citée dans une
offre d'emploi ajoute une charge opérationnelle réelle pour un bénéfice qui, à cette échelle,
reste théorique.

## Ce qu'il faut retenir

AKS ne rend pas Kubernetes simple, il rend son plan de contrôle géré — la complexité de ce
que vous y déployez reste entière. La décision de l'adopter se prend sur le nombre de services
et d'équipes à coordonner, pas sur la popularité de l'outil : le bon choix, souvent, est le
service managé le plus simple qui couvre le besoin réel.
