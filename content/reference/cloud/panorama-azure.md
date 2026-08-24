# Panorama Azure : les services qui reviennent tout le temps

Azure compte plus de deux cents services : personne ne les mémorise tous, et ce n'est pas le
but. Ce guide donne une carte de lecture — à quelle question chaque service répond — pour
situer un nom rencontré dans une documentation plutôt que le découvrir à l'aveugle.

## La structure avant les services : abonnement et groupe de ressources

Un **abonnement** (subscription) est la frontière de facturation et d'accès : c'est là que
s'appliquent quotas et permissions de haut niveau. Un **groupe de ressources** (resource
group) est un conteneur logique à l'intérieur d'un abonnement, qui rassemble tout ce qui
appartient à une même application — supprimer le groupe supprime tout ce qu'il contient,
d'un coup, sans en oublier une partie. Créer un groupe de ressources par application (et non
un seul groupe fourre-tout) est la première décision qui évite un ménage douloureux six mois
plus tard.

## Héberger du code : trois façons, trois curseurs de contrôle

**App Service** exécute une application web ou une API dans un environnement PaaS géré :
c'est le point de départ par défaut pour un backend .NET classique. **Container Apps**
exécute des conteneurs sans exposer la complexité d'un cluster Kubernetes — mise à l'échelle
automatique, scale-to-zero, révisions — pour qui a déjà une image Docker mais pas besoin de
piloter des Pods. **Functions** exécute du code déclenché par un événement (requête HTTP,
message de file, minuterie), facturé à l'exécution : adapté à du traitement ponctuel ou peu
fréquent, pas à un service qui doit répondre en continu avec une latence stable.

## Stocker des données : structurée, relationnelle, ou fichiers

**Azure SQL Database** est un SQL Server managé : sauvegardes automatiques, mise à l'échelle
du niveau de service, pas de serveur à administrer. **Azure Storage** couvre plusieurs
besoins sous un même service : Blob Storage pour des fichiers non structurés (images,
journaux, exports), Table Storage pour des données clé-valeur à très grande échelle, Queue
Storage pour découpler deux composants par une file de messages. Le choix entre Azure SQL et
Storage n'est pas une question de préférence : une donnée qui a besoin de jointures et de
transactions va en base relationnelle, un fichier ou un blob volumineux ne va jamais en base
relationnelle.

## Protéger un secret : Key Vault et l'identité managée

**Key Vault** stocke chaînes de connexion, clés et certificats hors du code source et hors
de la configuration en clair. Seul, il resterait un coffre de plus à sécuriser par un mot de
passe — c'est l'**identité managée** qui referme la boucle : elle donne à une ressource Azure
(une App Service, une Function) une identité que Key Vault reconnaît nativement, sans qu'
aucun secret ne transite pour l'obtenir. Le sujet est développé pour lui-même dans le dernier
guide de ce chapitre.

## Savoir ce qui se passe : Azure Monitor

**Azure Monitor** collecte métriques et journaux de toutes les ressources d'un abonnement, et
**Application Insights** (son volet applicatif) ajoute le traçage distribué d'une requête à
travers plusieurs services. Sans cette couche, un incident en production se diagnostique à
l'aveugle ; avec elle, une requête lente se retrace jusqu'à l'appel de base de données qui l'a
ralentie.

## Ce qu'il faut retenir

Un groupe de ressources borne une application, pas un abonnement entier. Le choix
d'hébergement suit la nature de la charge — continue, événementielle, ou déjà conteneurisée
— plus qu'une préférence personnelle. Une donnée relationnelle et un fichier ne vivent jamais
au même endroit. Et aucun de ces services ne remplace la surveillance : un système qu'on ne
peut pas observer est un système qu'on ne peut pas dépanner.
